#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace McpToolForUnity
{
	internal static class Settings
	{
		const string keyPort = "McpToolForUnity.Port";
		const string keyEnabled = "McpToolForUnity.Enabled";

		internal static int Port
		{
			get
			{
				var portStr = EditorUserSettings.GetConfigValue(keyPort) ?? "5000";
				if (!ushort.TryParse(portStr, out var port)) port = 5000;
				return port;
			}
			set
			{
				if (value != Port)
				{
					EditorUserSettings.SetConfigValue(keyPort, value.ToString());
					EditorMcp.Stop();
					EditorMcp.Start();
				}
			}
		}

		internal static bool Enabled
		{
			get => EditorUserSettings.GetConfigValue(keyEnabled) != "false";
			set
			{
				EditorUserSettings.SetConfigValue(keyEnabled, value ? "true" : "false");
				if (value)
					EditorMcp.Start();
				else
					EditorMcp.Stop();
			}
		}

		static string Command => $"{Path.GetFullPath("McpCommand/McpCommand.exe")} {Port}";

		[SettingsProvider]
		static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new SettingsProvider("Preferences/Mcp Tool", SettingsScope.User)
			{
				label = "Mcp Tool",
				guiHandler = _ =>
				{
					Enabled = EditorGUILayout.Toggle("Enabled", Enabled);
					if (Enabled)
					{
						Port = (ushort)EditorGUILayout.IntField("Port", Port);
						EditorGUILayout.TextField("Command", Command);
						EditorGUILayout.LabelField(" ", "👆paste to cursor MCP Server Config (Command)");
					}
				},
				keywords = new[]
				{
					"MCP",
					"Tool",
					"Unity",
					"Cursor",
					"Windsurf"
				}
			};

			return provider;
		}
	}
}
#endif