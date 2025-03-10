using System.IO;
using UnityEditor;

namespace McpToolForUnity
{
	static class Settings
	{
		const string key = "McpToolForUnity.Port";

		internal static int Port
		{
			get
			{
				var portStr = EditorUserSettings.GetConfigValue(key) ?? "5000";
				if (!ushort.TryParse(portStr, out var port)) port = 5000;
				return port;
			}
			set => EditorUserSettings.SetConfigValue(key, value.ToString());
		}

		static string Command => $"{Path.GetFullPath("McpCommand/McpCommand.exe")} {Port}";

		[SettingsProvider]
		static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new SettingsProvider("Preferences/Mcp Tool", SettingsScope.User)
			{
				label = "Mcp Tool",
				guiHandler = static _ =>
				{
					Port = (ushort)EditorGUILayout.IntField("Port", Port);
					EditorGUILayout.TextField("Command", Command);
					EditorGUILayout.LabelField(" ", "👆copy to cursor MCP Server Config (Command)");
				},
				keywords = new[]
				{
					"MCP",
					"Tool",
					"Unity",
					"Cursor",
					"Windsurf",
				},
			};

			return provider;
		}
	}
}
