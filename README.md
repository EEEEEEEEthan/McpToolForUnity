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

![image](https://github.com/user-attachments/assets/b10160c7-9d39-432e-a2c6-d87233fc7592)

## Installation

### 1. Unity Package

Add the package from the git URL: https://github.com/EEEEEEEEthan/McpToolForUnity.git#unity-package

This will create a McpCommand directory at the same level as Assets.

### 2. Attribute Everything

Add the [McpTool] attribute to the static methods you want to call.
```csharp
[McpTool("Add two numbers")]
static int Add([McpTool("The first one")] int a, [McpTool("The second one")] int b)
{
    // Attribute for parameters are not necessary
    return a + b;
}
```
![image](https://github.com/user-attachments/assets/77a1c319-a6cc-497e-93ca-59166da4a2a6)

### 3. Configure Cursor

Add Mcp settings to Cursor.

```
Name: Any
Type: command
Command: {project_path}/McpCommand/McpCommand.exe 5000 {project_path}/McpCommand/log.txt
```
Replace `{project_path}` with the path to your Unity project.

![image](https://github.com/user-attachments/assets/260ac691-de65-43e6-ba97-0c04dad43a64)

![image](https://github.com/user-attachments/assets/346f3d13-7ff9-4377-b995-26fe09cf9352)

### 4. Test Agent

![image](https://github.com/user-attachments/assets/b10160c7-9d39-432e-a2c6-d87233fc7592)
