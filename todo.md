# Roslyn MCP Server Implementation Plan

## Phase 1: Core Infrastructure & Basic Type Resolution
**Goal**: Establish the foundation and implement basic type information retrieval

### Tasks
- [x] Implement MCP server boilerplate and protocol handling
- [x] Create core models (SymbolInfo, MemberInfo, LocationInfo)
- [x] Implement SolutionCache with basic caching functionality
- [x] Implement WorkspaceResolver for solution discovery
- [x] Create RoslynService base class with solution loading
- [x] Implement `initialize_workspace` tool
- [x] Implement `get_type_at_position` tool

**Deliverables**: Working MCP server that can load solutions and resolve type information at cursor positions

---

## Phase 2: Member Discovery & Navigation
**Goal**: Enable code navigation and member exploration

### Tasks
- [ ] Implement `get_available_members` tool with filtering
- [ ] Add support for extension methods discovery
- [ ] Implement `find_symbol_definition` tool
- [ ] Implement `find_references` tool
- [ ] Add file system watching for cache invalidation
- [ ] Add comprehensive error handling with standardized error codes

**Deliverables**: Full navigation capabilities with performance optimization

---

## Phase 3: Advanced Analysis & Type Hierarchy
**Goal**: Provide deeper code analysis and type relationship understanding

### Tasks
- [ ] Implement `find_implementations` tool
- [ ] Implement `get_type_hierarchy` tool
- [ ] Implement `analyze_code_block` tool with diagnostics
- [ ] Implement `get_compilation_diagnostics` tool

**Deliverables**: Complete type analysis and diagnostics capabilities



# Roslyn MCP Server Specification

## Overview

Build an MCP (Model Context Protocol) server that provides semantic code intelligence for .NET projects using Roslyn. The server should expose tools that help AI assistants understand and navigate C# code by providing type information, member discovery, and code analysis capabilities.

## Roslyn Service Design

### SolutionCache Class

```csharp
public class SolutionCache
{
    private readonly ConcurrentDictionary<string, CachedSolution> _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    
    public class CachedSolution
    {
        public Solution Solution { get; set; }
        public DateTime LastAccessed { get; set; }
        public FileSystemWatcher Watcher { get; set; }
    }
    
    public async Task<Solution> GetSolutionAsync(string solutionPath);
    public void InvalidateSolution(string solutionPath);
    public void StartFileWatching(string solutionPath);
}
```

### RoslynService Class

```csharp
public class RoslynService
{
    private readonly SolutionCache _solutionCache;
    private readonly WorkspaceResolver _workspaceResolver;
    
    // Workspace management
    public async Task<WorkspaceInfo> InitializeWorkspaceAsync(
        string workingDirectory = null, string preferredSolution = null);
    
    public async Task<string> ResolveEffectiveSolutionPath(
        string filePath, string explicitSolutionPath = null);
    
    // Tool implementations - note solutionPath is now optional
    public async Task<SymbolInfo> GetTypeAtPositionAsync(
        string filePath, int line, int column, string solutionPath = null);
    
    public async Task<IEnumerable<MemberInfo>> GetAvailableMembersAsync(
        string filePath, int line, int column,
        bool includeExtensionMethods = true, bool includeStatic = true,
        string filter = null, string solutionPath = null);
    
    // Additional methods for each tool
}
```

### WorkspaceResolver Class

```csharp
public class WorkspaceResolver
{
    private WorkspaceInfo _currentWorkspace;
    private readonly Dictionary<string, string> _fileToSolutionCache;
    
    public async Task<string> FindSolutionForFileAsync(string filePath)
    {
        // 1. Check cache
        if (_fileToSolutionCache.TryGetValue(filePath, out var cached))
            return cached;
            
        // 2. Search upward from file
        var solution = await SearchUpwardForSolution(filePath);
        if (solution != null)
        {
            _fileToSolutionCache[filePath] = solution;
            return solution;
        }
        
        // 3. Use initialized workspace default
        if (_currentWorkspace?.PrimarySolution != null)
            return _currentWorkspace.PrimarySolution.Path;
            
        // 4. Search from current directory
        return await SearchFromCurrentDirectory();
    }
    
    private async Task<string> SearchUpwardForSolution(string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        
        while (!string.IsNullOrEmpty(directory))
        {
            // Check for .sln files
            var slnFiles = Directory.GetFiles(directory, "*.sln");
            if (slnFiles.Length == 1)
                return slnFiles[0];
                
            // Check for .csproj if no .sln found
            if (slnFiles.Length == 0)
            {
                var csprojFiles = Directory.GetFiles(directory, "*.csproj");
                if (csprojFiles.Length == 1)
                    return csprojFiles[0];
            }
            
            // Move up one directory
            directory = Directory.GetParent(directory)?.FullName;
        }
        
        return null;
    }
}
```

## MCP Tools Specification

### 1. initialize_workspace

**Description**: Initialize or detect the workspace and solution. Should be called once at the start of a session.

**Parameters**:
- `workingDirectory` (string, optional): Directory to search from (defaults to current working directory)
- `preferredSolution` (string, optional): Preferred solution if multiple are found

**Response**:
```json
{
  "success": true,
  "data": {
    "primarySolution": {
      "path": "/path/to/MySolution.sln",
      "name": "MySolution",
      "projectCount": 5
    },
    "allSolutions": [
      {
        "path": "/path/to/MySolution.sln",
        "projects": ["Core", "Web", "Tests"]
      }
    ],
    "frameworkVersions": [".NET 8.0", ".NET Standard 2.1"],
    "workspaceRoot": "/path/to/workspace"
  }
}
```

### 2. get_type_at_position

**Description**: Get type information at a specific position in the code.

**Parameters**:
- `filePath` (string, required): Path to source file (absolute or relative to workspace)
- `line` (int, required): 1-based line number
- `column` (int, required): 1-based column number
- `solutionPath` (string, optional): Override solution path (uses initialized workspace if not provided)

**Response**:
```json
{
  "success": true,
  "data": {
    "symbolName": "List<string>",
    "fullTypeName": "System.Collections.Generic.List<System.String>",
    "kind": "Class",
    "assembly": "System.Collections",
    "namespace": "System.Collections.Generic",
    "documentation": "Represents a strongly typed list...",
    "isGeneric": true,
    "typeArguments": ["System.String"],
    "baseType": "System.Object",
    "interfaces": ["IList<T>", "ICollection<T>", "IEnumerable<T>"],
    "resolvedFromSolution": "/path/to/MySolution.sln"
  }
}
```

### 3. get_available_members

**Description**: Get all accessible members at a specific code position.

**Parameters**:
- `filePath` (string, required): Path to source file
- `line` (int, required): 1-based line number
- `column` (int, required): 1-based column number
- `includeExtensionMethods` (bool, optional, default: true)
- `includeStatic` (bool, optional, default: true)
- `filter` (string, optional): Filter by name prefix
- `solutionPath` (string, optional): Override solution path

**Response**:
```json
{
  "success": true,
  "data": {
    "members": [
      {
        "name": "Add",
        "kind": "Method",
        "signature": "void Add(string item)",
        "declaringType": "System.Collections.Generic.List<System.String>",
        "accessibility": "public",
        "documentation": "Adds an object to the end of the List<T>.",
        "parameters": [
          {
            "name": "item",
            "type": "string",
            "documentation": "The object to be added"
          }
        ],
        "isExtension": false,
        "isStatic": false,
        "isAsync": false
      }
    ],
    "totalCount": 65,
    "filteredCount": 1
  }
}
```

### 3. find_symbol_definition

**Description**: Find the definition location of a symbol.

**Parameters**:
- `solutionPath` (string, required)
- `filePath` (string, required)
- `line` (int, required)
- `column` (int, required)

**Response**:
```json
{
  "success": true,
  "data": {
    "definitionLocation": {
      "filePath": "Services/UserService.cs",
      "line": 15,
      "column": 17,
      "endLine": 15,
      "endColumn": 28
    },
    "symbolInfo": {
      "name": "GetUserAsync",
      "kind": "Method",
      "containingType": "UserService"
    },
    "sourceText": "public async Task<User> GetUserAsync(int id)"
  }
}
```

### 4. find_references

**Description**: Find all references to a symbol.

**Parameters**:
- `solutionPath` (string, required)
- `filePath` (string, required)
- `line` (int, required)
- `column` (int, required)
- `includeDeclaration` (bool, optional, default: false)
- `maxResults` (int, optional, default: 100)

**Response**:
```json
{
  "success": true,
  "data": {
    "references": [
      {
        "filePath": "Controllers/UserController.cs",
        "line": 23,
        "column": 25,
        "lineText": "var user = await _userService.GetUserAsync(id);",
        "kind": "MethodCall"
      }
    ],
    "totalCount": 5,
    "hasMore": false
  }
}
```

### 5. find_implementations

**Description**: Find all implementations of an interface or derived types.

**Parameters**:
- `solutionPath` (string, required)
- `typeName` (string, required): Fully qualified type name
- `findDerivedTypes` (bool, optional, default: true)
- `findInterfaceImplementations` (bool, optional, default: true)

**Response**:
```json
{
  "success": true,
  "data": {
    "implementations": [
      {
        "typeName": "SqlUserRepository",
        "fullTypeName": "MyApp.Data.SqlUserRepository",
        "filePath": "Data/SqlUserRepository.cs",
        "line": 8,
        "kind": "Class",
        "implementsDirectly": true
      }
    ]
  }
}
```


### 6. get_method_overloads

**Description**: Get all overloads of a method.

**Parameters**:
- `solutionPath` (string, required)
- `filePath` (string, required)
- `line` (int, required)
- `column` (int, required)

**Response**:
```json
{
  "success": true,
  "data": {
    "overloads": [
      {
        "signature": "Task<User> GetUserAsync(int id)",
        "parameters": [{"name": "id", "type": "int"}],
        "documentation": "Gets a user by ID"
      },
      {
        "signature": "Task<User> GetUserAsync(string email)",
        "parameters": [{"name": "email", "type": "string"}],
        "documentation": "Gets a user by email"
      }
    ]
  }
}
```

### 7. get_type_hierarchy

**Description**: Get inheritance hierarchy for a type.

**Parameters**:
- `solutionPath` (string, required)
- `typeName` (string, required): Fully qualified type name
- `direction` (string, optional): "Base", "Derived", or "Both" (default: "Both")

**Response**:
```json
{
  "success": true,
  "data": {
    "baseTypes": [
      {
        "typeName": "BaseRepository",
        "fullTypeName": "MyApp.Data.BaseRepository",
        "assembly": "MyApp.Data"
      }
    ],
    "derivedTypes": [
      {
        "typeName": "CachedUserRepository",
        "fullTypeName": "MyApp.Data.CachedUserRepository",
        "assembly": "MyApp.Data"
      }
    ],
    "interfaces": [
      {
        "typeName": "IUserRepository",
        "fullTypeName": "MyApp.Core.IUserRepository"
      }
    ]
  }
}
```

## Usage Patterns for Claude Code

### Typical Session Flow

1. **Initial Setup** (Claude Code would likely do this automatically):
```
→ initialize_workspace()
← Returns primary solution and workspace info
```

2. **Exploring Code** (no solution path needed):
```
→ get_type_at_position(filePath: "Controllers/UserController.cs", line: 25, column: 30)
← Returns type info using auto-discovered solution

→ get_available_members(filePath: "Controllers/UserController.cs", line: 25, column: 30)
← Returns available members
```


### Auto-Discovery Logic

The resolution order for finding the appropriate solution:

1. **Explicit solution path** (if provided in the tool call)
2. **File-based discovery** (search upward from the file path)
3. **Cached workspace** (from initialize_workspace)
4. **Current directory search** (as fallback)

### Benefits

- **Reduces friction**: Claude Code doesn't need to track solution paths
- **Natural workflow**: Just provide the file being worked on
- **Smart defaults**: Works correctly 95% of the time without explicit paths
- **Override available**: Can still specify solution when needed

### Performance Optimization

1. **Solution Caching**:
   - Cache loaded solutions for 30 minutes
   - Use file system watchers to invalidate cache on changes
   - Implement lazy loading of documents

2. **Cancellation Support**:
   - All async operations should accept CancellationToken
   - Implement timeouts for long-running operations (default: 30 seconds)

3. **Memory Management**:
   - Limit number of cached solutions (default: 5)
   - Implement LRU eviction for solution cache
   - Dispose of Roslyn workspaces properly

### Error Handling

1. Return standardized error responses:
```json
{
  "success": false,
  "error": {
    "code": "SOLUTION_NOT_FOUND",
    "message": "Solution file not found: MyApp.sln",
    "details": {
      "searchedPath": "/path/to/MyApp.sln"
    }
  }
}
```

2. Common error codes:
   - `SOLUTION_NOT_FOUND`
   - `FILE_NOT_FOUND`
   - `INVALID_POSITION`
   - `COMPILATION_FAILED`
   - `TIMEOUT`
   - `SYMBOL_NOT_FOUND`


## Dependencies

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
<PackageReference Include="Microsoft.Build.Locator" Version="1.6.10" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```
