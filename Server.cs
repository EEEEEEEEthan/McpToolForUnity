using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace McpToolForUnity
{
	internal sealed class Server : IDisposable
	{
		static string GetTypeName(Type type)
		{
			if (type == typeof(int)) return "number";
			if (type == typeof(float)) return "number";
			if (type == typeof(double)) return "number";
			if (type == typeof(string)) return "string";
			return "object";
		}

		[SuppressMessage("ReSharper", "NotAccessedField.Local"),
		 SuppressMessage("ReSharper", "CollectionNeverQueried.Local")]
		struct Tool
		{
			[JsonIgnore] public MethodInfo methodInfo;
			public string name;
			public string description;
			public InputSchema inputSchema;
			public List<string> required;
		}

		[SuppressMessage("ReSharper", "NotAccessedField.Local"),
		 SuppressMessage("ReSharper", "CollectionNeverQueried.Local")]
		struct InputSchema
		{
			public string type;
			public Dictionary<string, PropertyInfo> properties;
		}

		[SuppressMessage("ReSharper", "NotAccessedField.Local"),
		 SuppressMessage("ReSharper", "CollectionNeverQueried.Local")]
		struct PropertyInfo
		{
			public string type;
			public string description;
		}

		enum LogType
		{
			Log,
			Error
		}

		readonly Queue<Action> requestQueue = new Queue<Action>();
		readonly Dictionary<string, Tool> allTools = new Dictionary<string, Tool>();
		readonly int port;
		readonly TcpListener tcpListener;
		bool disposed;

		public Server(int port = 5000) => tcpListener = new TcpListener(IPAddress.Any, port);

		public void Start()
		{
			if (disposed) throw new ObjectDisposedException(nameof(Server));
			var thread = new Thread(start)
			{
				IsBackground = true
			};
			thread.Start();

			void start()
			{
				// launch server
				tcpListener.Start();
				while (true)
					try
					{
						var client = tcpListener.AcceptTcpClient();
						var thread = new Thread(() => HandleClient(client))
						{
							IsBackground = true
						};
						thread.Start();
					}
					catch (Exception e)
					{
						//Debug.LogException(e);
					}
				// ReSharper disable once FunctionNeverReturns
			}
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			tcpListener.Stop();
		}

		/// <summary>
		///     deal with unhandled messages
		/// </summary>
		public void Update()
		{
			while (true)
			{
				Action action;
				lock (requestQueue)
				{
					if (requestQueue.Count > 0)
						action = requestQueue.Dequeue();
					else
						break;
				}
				try
				{
					action();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		public void RegisterTool(string alias, MethodInfo method)
		{
			if (!method.IsStatic)
			{
				Debug.LogError(
					$"Attribute {nameof(McpToolAttribute)} can only be applied to static methods. {method.Name}");
				return;
			}
			var methodAttributes = method.GetCustomAttributes(typeof(McpToolAttribute), false);
			var desc = methodAttributes.Length <= 0 ? "" : ((McpToolAttribute)methodAttributes[0]).Description;
			var tool = new Tool
			{
				methodInfo = method,
				name = method.Name,
				description = desc,
				inputSchema = new InputSchema
				{
					type = "object",
					properties = new Dictionary<string, PropertyInfo>()
				},
				required = new List<string>()
			};
			foreach (var parameter in method.GetParameters())
			{
				var parameterAttributes = parameter.GetCustomAttributes(typeof(McpToolAttribute), false);
				if (parameterAttributes.Length > 0)
				{
					var parameterAttribute = (McpToolAttribute)parameterAttributes[0];
					tool.inputSchema.properties[parameter.Name] = new PropertyInfo
					{
						type = GetTypeName(parameter.ParameterType),
						description = parameterAttribute.Description
					};
				}
				else
				{
					tool.inputSchema.properties[parameter.Name] = new PropertyInfo
					{
						type = GetTypeName(parameter.ParameterType),
						description = ""
					};
				}
				tool.required.Add(parameter.Name);
			}
			lock (allTools)
			{
				if (allTools.ContainsKey(alias))
					Debug.LogError($"MCPTool with name {tool.name} already exists.");
				else
					allTools.Add(alias, tool);
			}
		}

		JObject Handle(JObject input)
		{
			// https://model-context-protocol.github.io/specification/basic
			object res = null;
			if (input.TryGetValue("method", out var methodToken))
			{
				var methodTokenValue = methodToken.Value<string>();
				switch (methodTokenValue)
				{
					case "initialize":
						res = new
						{
							protocolVersion = input["params"]["protocolVersion"],
							capabilities = new
							{
								logging = new { },
								prompts = new
								{
									listChanged = true
								},
								resources = new
								{
									subscribe = true,
									listChanged = true
								},
								tools = new
								{
									listChanged = true
								}
							},
							serverInfo = new
							{
								name = "unity",
								version = "1.0.0"
							}
						};
						break;
					case "tools/list":
						res = new
						{
							tools = allTools.Values.ToArray()
						};
						break;
					case "tools/call":
					{
						// {"method":"tools/call","params":{"name":"Update","arguments":{}},"jsonrpc":"2.0","id":2}
						var @params = input["params"];
						var methodName = @params["name"].Value<string>();
						var tool = allTools[methodName];
						var paramList = new List<object>();
						foreach (var parameter in tool.methodInfo.GetParameters())
						{
							var arguments = @params["arguments"];
							paramList.Add(arguments[parameter.Name].ToObject(parameter.ParameterType));
						}
						var v = tool.methodInfo.Invoke(null, paramList.ToArray());
						if (tool.methodInfo.ReturnType == typeof(void))
							v = "success";
						res = new
						{
							content = new[]
							{
								new
								{
									type = "text",
									text = v.ToString()
								}
							},
							isError = false
						};
						break;
					}
				}
			}
			object response;
			if (input.TryGetValue("id", out var idValue))
				response = new
				{
					jsonrpc = "2.0",
					id = idValue.Value<int>(),
					result = res
				};
			else
				response = new
				{
					jsonrpc = "2.0",
					result = res
				};
			var jObject = JObject.FromObject(response);
			return jObject;
		}

		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		void HandleClient(TcpClient client)
		{
			//Debug.Log($"Client connected: {client.Client.RemoteEndPoint}");
			var stream = client.GetStream();
			using var reader = new BinaryReader(stream);
			using var writer = new BinaryWriter(stream);
			var disposed = false;
			while (client.Connected)
				if (client.GetStream().DataAvailable)
					try
					{
						var length = reader.ReadInt32();
						var bytes = reader.ReadBytes(length);
						var text = System.Text.Encoding.UTF8.GetString(bytes);
						Debug.Log($"Txt Received: {text}");
						var jsonObject = JsonConvert.DeserializeObject<JObject>(text);
						lock (requestQueue)
						{
							requestQueue.Enqueue(() => respond(jsonObject));
						}
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				else Thread.Sleep(100);
			disposed = true;
			//Debug.Log($"Client disconnected: {client.Client.RemoteEndPoint}");

			void respond(JObject input)
			{
				// ReSharper disable once AccessToModifiedClosure
				if (!client.Connected || disposed) return;
				//Debug.Log($"Mcp Received: {input}");
				var output = Handle(input);
				//Debug.Log($"Mcp Respond: {output.ToString(Formatting.Indented)}");
				var bytes = System.Text.Encoding.UTF8.GetBytes(output.ToString(Formatting.None));
				writer.Write(bytes.Length);
				writer.Write(bytes);
				writer.Flush();
			}
		}

		~Server()
		{
			Dispose();
		}
	}
}