using System;

namespace McpToolForUnity
{
    public sealed class McpToolAttribute : Attribute
    {
        public McpToolAttribute(string description) => Description = description;

        public string Description { get; }
    }
}