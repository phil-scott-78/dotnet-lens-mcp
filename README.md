# dotnet-lens-mcp

A Model Context Protocol (MCP) server that brings Roslyn-powered code intelligence to your AI assistant. 

## What does it do?

This MCP server lets AI assistants understand C# code deeply by providing semantic analysis tools:

- **Type resolution** - Hover over any variable or expression to see its actual type
- **Find definitions** - Jump to where symbols are defined, just like F12 in VS
- **Find references** - See everywhere a symbol is used across your solution
- **Explore APIs** - Get IntelliSense-style member listings at any code position
- **Navigate inheritance** - Find implementations, derived types, and base classes
- **Check build errors** - Get real compiler diagnostics for your code

## Getting Started

### From NuGet (Recommended)

Configure your AI assistant to use the published package:

```json
{
  "servers": {
    "dotnet-lens": {
      "type": "stdio",
      "command": "dnx",
      "args": ["dotnet-lens-mcp", "--version", "0.1.0-beta", "--yes"]
    }
  }
}
```

### From Source

For development or testing:

```json
{
  "servers": {
    "dotnet-lens": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/DotnetLensMcp"]
    }
  }
}
```

## Usage

Once configured, your AI assistant gains these capabilities:

1. **First, initialize the workspace** - The AI will scan for .sln or .csproj files
2. **Then use any analysis tools** - Get type info, find references, check for errors, etc.

Example prompts:
- "Find all places where this method is called"
- "Show me what methods I can call on this object"
- "What classes implement this interface?"

## Requirements

- .NET 9.0 or later
- A C# project or solution to analyze

## Feedback & Issues

Found a bug? Not surprised! Please open an issue on our [GitHub repository](https://github.com/phil-scott-78/dotnet-lens-mcp).