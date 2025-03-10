# Introduction to McpToolForUnity

## Features

Simply add the [McpTool] attribute to static methods to make them callable from editors like Cursor/windsurf.
```csharp
[McpTool("Add two numbers")]
static int Add([McpTool("The first one")] int a, [McpTool("The second one")] int b)
{
    // Attribute for parameters are not necessary
    return a + b;
}
```

![image](https://github.com/user-attachments/assets/1966a4ae-bf73-440b-8ecc-3108b368064d)

## Installation

### 1. Unity Package

Add the package from the git URL: https://github.com/EEEEEEEEthan/McpToolForUnity.git

This will create a McpCommand directory at the same level as Assets.

### 2. Open Unity Preferences

![image](https://github.com/user-attachments/assets/828f3f3e-1ade-41d7-aa65-7d78a92b82eb)

restart Unity if you have modified `port`

### 3. Configure Cursor

copy command to Cursor settings

![image](https://github.com/user-attachments/assets/1182cdfc-7cf1-4f0b-bf4e-4b401e086919)

```
Name: Any
Type: command
Command: {project_path}/McpCommand/McpCommand.exe
```
Replace `{project_path}` with the path to your Unity project.

![image](https://github.com/user-attachments/assets/260ac691-de65-43e6-ba97-0c04dad43a64)

![image](https://github.com/user-attachments/assets/346f3d13-7ff9-4377-b995-26fe09cf9352)

### 4. Attribute Everything

Add the [McpTool] attribute to the static methods you want to call.
```csharp
[McpTool("Add two numbers")]
static int Add([McpTool("The first one")] int a, [McpTool("The second one")] int b)
{
    // Attribute for parameters are not necessary
    return a + b;
}
```

you need re-enable cursor MCP server to refresh tool list

### 5. Test Agent

![image](https://github.com/user-attachments/assets/1966a4ae-bf73-440b-8ecc-3108b368064d)
