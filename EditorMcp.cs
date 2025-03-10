#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace McpToolForUnity
{
	[InitializeOnLoad]
	internal static class EditorMcp
	{
		static EditorMcp()
		{
			var server = new Server(8080);
			new Thread(registerTools).Start();
			server.Start();
			AssemblyReloadEvents.beforeAssemblyReload += () => server.Dispose();
			EditorApplication.quitting += () => server.Dispose();
			copyFiles();

			void registerTools()
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
								Debug.LogError(
									$"Attribute {nameof(McpToolAttribute)} can only be applied to static methods. {type.FullName}.{method.Name}");
								continue;
							}
							server.RegisterTool(method.Name, method);
						}
					}
				}
				EditorApplication.update += update;

				void update()
				{
					server.Update();
				}
			}

			static void copyFiles()
			{
				try
				{
					var path = Path.Combine(Application.dataPath, "..", "McpCommand");
					var targetPath = Path.GetFullPath(path);
					var editorFolderPath = AssetDatabase.GUIDToAssetPath("e209e4ef4d7ff2747a70c33babf2295e");
					var packagePath = Path.GetDirectoryName(editorFolderPath);
					var toolPath = Path.Combine(packagePath, ".Client");
					toolPath = Path.GetFullPath(toolPath);
					// Copy toolPath folder to targetPath folder (overwrite)
					Directory.CreateDirectory(targetPath);
					foreach (var file in Directory.GetFiles(toolPath, "*", SearchOption.AllDirectories))
					{
						var fileName = Path.GetFileName(file);
						File.Copy(file, $"{targetPath}/{fileName}", true);
					}
				}
				catch (Exception e)
				{
				}
			}
		}

		[McpTool("launch game")]
		static void LaunchGame()
		{
			EditorApplication.isPlaying = true;
		}

		[McpTool("stop game")]
		static void StopGame()
		{
			EditorApplication.isPaused = false;
		}

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
	}
}
#endif