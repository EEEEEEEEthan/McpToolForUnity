using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using McpServer;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace McpToolForUnity.Editor
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
			//server.Out = new UnityLogWriter();
			server.Error = new UnityErrorWriter();
			new Thread(registerTools).Start();
			server.Start();
			copyFiles();
			//Debug.Log("Mcp server started.");

			static void registerTools()
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
				EditorApplication.update += update;

				static void update()
				{
					server.Update();
				}
			}

			static void copyFiles()
			{
				var path = Path.Combine(Application.dataPath, "..", "McpCommand");
				var targetPath = Path.GetFullPath(path);
				var editorFolderPath = AssetDatabase.GUIDToAssetPath("36e3a6be7d0292c4a82ea0b9f7e79e00");
				var packagePath = Path.GetDirectoryName(editorFolderPath);
				var toolPath = Path.Combine(packagePath, ".Command");
				toolPath = Path.GetFullPath(toolPath);
				// Copy toolPath folder to targetPath folder (overwrite)
				Directory.CreateDirectory(targetPath);
				foreach (var file in Directory.GetFiles(toolPath, "*", SearchOption.AllDirectories))
				{
					var relativePath = Path.GetRelativePath(toolPath, file);
					var destFile = Path.Combine(targetPath, relativePath);
					var destDir = Path.GetDirectoryName(destFile);
					Directory.CreateDirectory(destDir);
					File.Copy(file, destFile, true);
				}
				Debug.Log($"Copied files from {toolPath} to {targetPath}");
			}
		}
	}
}
