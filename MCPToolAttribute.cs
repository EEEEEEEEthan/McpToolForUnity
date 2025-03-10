using System;

namespace McpToolForUnity
{
	public sealed class McpToolAttribute : Attribute
	{
		public string Description { get; }
		public McpToolAttribute(string description) => Description = description;
	}
}
