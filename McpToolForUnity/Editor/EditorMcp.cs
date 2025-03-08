using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using McpServer;
using UnityEditor;
using UnityEngine;

namespace Game.Vision.EditorTools
{
	[InitializeOnLoad]
	static class EditorMcp
	{
		sealed class UnityLogWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;

			public override void Write(char value)
			{
				Debug.Log(value);
			}

			public override void Write(string value)
			{
				Debug.Log(value);
			}
		}

		sealed class UnityErrorWriter : TextWriter
		{
			public override Encoding Encoding => Encoding.Default;

			public override void Write(char value)
			{
				Debug.LogError(value);
			}

			public override void Write(string value)
			{
				Debug.LogError(value);
			}
		}

		static readonly Server server;

		static EditorMcp()
		{
			server = new();
			server.threadedLog = false;
			server.Out = new UnityLogWriter();
			server.Error = new UnityErrorWriter();
			new Thread(register).Start();
			server.Start();

			static void register()
			{
				// build tool list
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var assembly in assemblies)
				{
					foreach (var type in assembly.GetTypes())
					{
						foreach (var method in type.GetMethods((BindingFlags)0xffff))
						{
							var methodAttributes = method.GetCustomAttributes(typeof(McpToolAttribute), false);
							if (methodAttributes.Length <= 0) continue;
							if (!method.IsStatic)
							{
								Debug.LogError($"Attribute {nameof(McpToolAttribute)} can only be applied to static methods. {type.FullName}.{method.Name}");
								continue;
							}
							server.RegisterTool(method.Name, method);
						}
					}
				}
				EditorApplication.update += Update;
			}
		}

		static void Update()
		{
			server.Update();
		}
	}
}
