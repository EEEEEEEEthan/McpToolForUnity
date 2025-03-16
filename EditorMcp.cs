#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace McpToolForUnity
{
	[InitializeOnLoad]
	internal static class EditorMcp
	{
		static Client client;

		static EditorMcp()
		{
			Start();
			AssemblyReloadEvents.beforeAssemblyReload += () => client?.Dispose();
			EditorApplication.quitting += () => client?.Dispose();
		}

		internal static void Stop()
		{
			client?.Dispose();
			client = null;
		}

		internal static async void Start()
		{
			await Task.Delay(3000);
			if (!Settings.Enabled) return;
			var port = Settings.Port;
			await Task.Run(() => StartAsync(port));
		}

		static void StartAsync(int port)
		{
			copyFiles();
			registerTools();
			client = new Client(port);
			//Debug.Log("client created");
			EditorApplication.update += update;

			void update()
			{
				client?.Update();
			}

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
							Client.RegisterTool(method.Name, method);
						}
					}
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
					// ignored
				}
			}
		}
	}
}
#endif