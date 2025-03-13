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
using Task = System.Threading.Tasks.Task;

namespace McpToolForUnity
{
	internal sealed class Client : IDisposable
	{
		static readonly Dictionary<string, Tool> allTools = new Dictionary<string, Tool>();

		public static void RegisterTool(string alias, MethodInfo method)
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

		static void Log(object message)
		{
			//Debug.Log(message);
		}

		static string GetTypeName(Type type)
		{
			if (type == typeof(int)) return "number";
			if (type == typeof(float)) return "number";
			if (type == typeof(double)) return "number";
			if (type == typeof(string)) return "string";
			return "object";
		}

		static JObject Handle(JObject input)
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
						lock (allTools)
						{
							res = new
							{
								tools = allTools.Values.ToArray()
							};
						}
						break;
					case "tools/call":
					{
						// {"method":"tools/call","params":{"name":"Update","arguments":{}},"jsonrpc":"2.0","id":2}
						var @params = input["params"];
						var methodName = @params["name"].Value<string>();
						Tool tool;
						lock (allTools)
						{
							tool = allTools[methodName];
						}
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

		readonly Queue<Action> requestQueue = new Queue<Action>();
		readonly int port;
		bool enabled = true;

		public Client(int port = 5000)
		{
			Task.Run(start);

			void start()
			{
				while (enabled)
				{
					try
					{
						using var client = new TcpClient();
						try
						{
							client.Connect(IPAddress.Loopback, port);
						}
						catch (SocketException)
						{
							Thread.Sleep(1000);
							continue;
						}
						Log("mcp client created");
						using var stream = client.GetStream();
						using var reader = new BinaryReader(stream);
						using var writer = new BinaryWriter(stream);
						var disposed = false;
						while (client.Connected)
						{
							var text = reader.ReadString();
							var jObject = JObject.Parse(text);
							Log(jObject.ToString());
							lock (requestQueue)
							{
								requestQueue.Enqueue(() =>
								{
									// ReSharper disable once AccessToModifiedClosure
									if (disposed) return;
									var result = Handle(jObject);
									// ReSharper disable once AccessToDisposedClosure
									writer.Write(result.ToString(Formatting.None));
									writer.Flush();
								});
							}
						}
						disposed = true;
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
					Thread.Sleep(10);
				}
			}
		}

		/// <summary>
		///     deal with unhandled messages
		/// </summary>
		public void Update()
		{
			while (enabled)
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

		public void Dispose()
		{
			if (enabled) return;
			enabled = false;
		}

		~Client()
		{
			Dispose();
		}
	}
}