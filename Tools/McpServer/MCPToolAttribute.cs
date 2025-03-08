using System;

namespace McpServer
{
    public sealed class McpToolAttribute : Attribute
    {
        public McpToolAttribute(string description) => Description = description;

        public string Description { get; }
    }
}