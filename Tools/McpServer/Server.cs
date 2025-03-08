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

namespace McpServer
{
    public sealed class Server
    {
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

        public bool threadedLog = true;
        readonly Queue<Action> requestQueue = new();
        readonly Dictionary<string, Tool> allTools = new();
        readonly Queue<(LogType type, string log)> logQueue = new();
        readonly int port;

        public Server(int port = 5000) => this.port = port;

        public TextWriter Out { get; set; }
        public TextWriter Error { get; set; }

        static string GetTypeName(Type type)
        {
            if (type == typeof(int)) return "number";
            if (type == typeof(float)) return "number";
            if (type == typeof(double)) return "number";
            if (type == typeof(string)) return "string";
            return "object";
        }

        public void Start()
        {
            var tcpListener = new TcpListener(IPAddress.Any, port);
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
                        LogError(e.Message);
                    }
                // ReSharper disable once FunctionNeverReturns
            }
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
                    LogError(e.Message);
                }
            }
            while (TryPrintLog()) ;
        }

        public void RegisterTool(string alias, MethodInfo method)
        {
            if (!method.IsStatic)
            {
                LogError(
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
                if (!allTools.TryAdd(alias, tool))
                    LogError($"MCPTool with name {tool.name} already exists.");
            }
        }

        void Log(object obj)
        {
            if (Out is null) return;
            if (!threadedLog)
            {
                Out.Write(obj);
                return;
            }
            lock (logQueue)
            {
                logQueue.Enqueue((LogType.Log, $"{obj}"));
            }
        }

        void LogError(object obj)
        {
            if (Error is null) return;
            if (!threadedLog)
            {
                Error.Write(obj);
                return;
            }
            lock (logQueue)
            {
                logQueue.Enqueue((LogType.Error, $"{obj}"));
            }
        }

        bool TryPrintLog()
        {
            (LogType type, string text) log;

            lock (logQueue)
            {
                if (!logQueue.TryDequeue(out log)) return false;
            }
            switch (log.type)
            {
                case LogType.Log:
                    Out.WriteLine(log.text);
                    break;
                case LogType.Error:
                default:
                    Error.WriteLine(log.text);
                    break;
            }
            return true;
        }

        JObject Handle(JObject input)
        {
            // https://model-context-protocol.github.io/specification/basic
            object res = null;
            if (input.TryGetValue("method", out var methodToken))
            {
                var methodTokenValue = methodToken.Value<string>();
                if (methodTokenValue == "initialize")
                {
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
                }
                else if (methodTokenValue == "tools/list")
                {
                    res = new
                    {
                        tools = allTools.Values.ToArray()
                    };
                }
                else if (methodTokenValue == "tools/call")
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

        void HandleClient(TcpClient client)
        {
            Log($"Client connected: {client.Client.RemoteEndPoint}");
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
                        Log($"Txt Received: {text}");
                        var jsonObject = JsonConvert.DeserializeObject<JObject>(text);
                        lock (requestQueue)
                        {
                            requestQueue.Enqueue(() => respond(jsonObject));
                        }
                    }
                    catch (Exception e)
                    {
                        LogError(e.Message);
                    }
                else Thread.Sleep(100);
            disposed = true;
            Log($"Client disconnected: {client.Client.RemoteEndPoint}");

            [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
            void respond(JObject input)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (!client.Connected || disposed) return;
                Log($"Mcp Received: {input}");
                var output = Handle(input);
                Log($"Mcp Respond: {output.ToString(Formatting.Indented)}");
                var bytes = System.Text.Encoding.UTF8.GetBytes(output.ToString(Formatting.None));
                writer.Write(bytes.Length);
                writer.Write(bytes);
                writer.Flush();
            }
        }

        enum LogType
        {
            Log,
            Error
        }
    }
}