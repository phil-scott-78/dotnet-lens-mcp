using System.ComponentModel;
using DotnetLensMcp.Models.Responses;
using DotnetLensMcp.SerializerExtensions;
using DotnetLensMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotnetLensMcp.Tools;

public class RoslynTools(
    RoslynService roslynService,
    ILogger<RoslynTools> logger)
{
    [McpServerTool] 
    [Description("""
                 Initialize C#/.NET workspace and solution detection. MUST be called first before using any other C#/.NET tools.

                 Use this when:
                 - Starting analysis of any C# or .NET project
                 - Working with .csproj, .sln, or .cs files
                 - Before performing any code analysis operations

                 Examples:
                 - User: 'Help me understand this C# project' -> Call InitializeWorkspace first
                 - User: 'Fix the build errors in my .NET app' -> Call InitializeWorkspace first
                 - User: 'Analyze the architecture of this solution' -> Call InitializeWorkspace first

                 Returns:
                 - Success: true/false
                 - Data.PrimarySolution: Main solution info (path, name, project count)
                 - Data.AllSolutions: All discovered solution files
                 - Data.FrameworkVersions: Detected .NET framework versions
                 - Data.WorkspaceRoot: Root directory of the workspace
                 """)]
    public async Task<string> InitializeWorkspace(
        [Description("Directory to search from (defaults to current working directory)")] string? workingDirectory = null,
        [Description("Preferred solution if multiple are found")] string? preferredSolution = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await roslynService.InitializeWorkspaceAsync(workingDirectory, preferredSolution);

            
            return new InitializeWorkspaceResponse
            {
                Success = true,
                Data = new InitializeWorkspaceData
                {
                    PrimarySolution = workspace.PrimarySolution != null ? new SolutionInfo
                    {
                        Path = workspace.PrimarySolution.Path,
                        Name = workspace.PrimarySolution.Name,
                        ProjectCount = workspace.PrimarySolution.ProjectCount
                    } : null,
                    AllSolutions = workspace.AllSolutions.Select(s => new
                    {
                        path = s.Path,
                        projects = s.Projects
                    }),
                    FrameworkVersions = workspace.FrameworkVersions,
                    WorkspaceRoot = workspace.WorkspaceRoot
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize workspace");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = "INITIALIZATION_FAILED",
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Get C# type information at a specific cursor position. Resolves the type of variables, expressions, method calls, etc.

                 Use this when:
                 - User asks 'What type is this variable?'
                 - Need to understand what a var keyword resolves to
                 - Analyzing method return types or parameter types
                 - Understanding generic type arguments
                 - Hovering over any symbol to get type info

                 Examples:
                 - Code: 'var result = GetUserAsync();' -> Use at 'result' position to find it's Task<User>
                 - Code: 'customers.Where(c => c.Active)' -> Use at 'Where' to see it returns IEnumerable<Customer>
                 - Code: 'new Dictionary<string, List<int>>()' -> Use to see full generic type info

                 Returns:
                 - SymbolName: The symbol at position (e.g., 'result', 'Where')
                 - FullTypeName: Complete type with namespace (e.g., 'System.Threading.Tasks.Task<MyApp.Models.User>')
                 - Kind: Symbol kind (Method, Property, Variable, etc.)
                 - Assembly: Which assembly defines this type
                 - Documentation: XML doc comments if available
                 - BaseType: What this type inherits from
                 - Interfaces: What interfaces it implements
                 """)]
    public async Task<string> GetTypeAtPosition(
        [Description("Path to source file (absolute or relative to workspace)")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path (uses initialized workspace if not provided)")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var symbolInfo = await roslynService.GetTypeAtPositionAsync(
                filePath, 
                line, 
                column, 
                solutionPath, 
                cancellationToken);

            if (symbolInfo == null)
            {
                return new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Code = "SYMBOL_NOT_FOUND",
                        Message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new GetTypeAtPositionResponse
            {
                Success = true,
                Data = new TypeAtPositionData
                {
                    SymbolName = symbolInfo.SymbolName,
                    FullTypeName = symbolInfo.FullTypeName,
                    Kind = symbolInfo.Kind,
                    Assembly = symbolInfo.Assembly,
                    Namespace = symbolInfo.Namespace,
                    Documentation = symbolInfo.Documentation,
                    IsGeneric = symbolInfo.IsGeneric,
                    TypeArguments = symbolInfo.TypeArguments,
                    BaseType = symbolInfo.BaseType,
                    Interfaces = symbolInfo.Interfaces,
                    ResolvedFromSolution = symbolInfo.ResolvedFromSolution
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type at position");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Get all available C# members (methods, properties, fields) accessible at a specific code position. Like IntelliSense autocomplete.

                 Use this when:
                 - User asks 'What methods can I call on this object?'
                 - Need to explore an API without documentation
                 - Writing code and need to know available members
                 - Checking if a specific method exists on a type
                 - Finding extension methods available for a type

                 Examples:
                 - Code: 'string text = "hello"; text.' -> Use at position after '.' to see all string methods
                 - Code: 'var list = new List<int>(); list.' -> Shows Add, Remove, Clear, Count, etc.
                 - Finding LINQ methods: Use with includeExtensionMethods=true on IEnumerable
                 - Filter example: filter="Start" on string type returns StartsWith, StartsWith overloads

                 Returns:
                 - Members: Array of available members with:
                   - Name: Member name (e.g., 'ToString', 'Length')
                   - Kind: Method, Property, Field, Event
                   - Signature: Full signature with parameters
                   - Documentation: XML doc summary
                   - IsExtension: True if it's an extension method
                   - Parameters: Parameter names, types, and docs
                 """)]
    public async Task<string> GetAvailableMembers(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include extension methods")] bool includeExtensionMethods = true,
        [Description("Include static members")] bool includeStatic = true,
        [Description("Filter by name prefix")] string? filter = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await roslynService.GetAvailableMembersAsync(
                filePath,
                line,
                column,
                includeExtensionMethods,
                includeStatic,
                filter,
                solutionPath,
                cancellationToken);

            var membersList = members.ToList();

            return new GetAvailableMembersResponse
            {
                Success = true,
                Data = new AvailableMembersData
                {
                    Members = membersList.Select(m => new MemberData
                    {
                        Name = m.Name,
                        Kind = m.Kind,
                        Signature = m.Signature,
                        DeclaringType = m.DeclaringType,
                        Accessibility = m.Accessibility,
                        Documentation = m.Documentation,
                        Parameters = m.Parameters?.Select(p => new ParameterData
                        {
                            Name = p.Name,
                            Type = p.Type,
                            Documentation = p.Documentation
                        }),
                        IsExtension = m.IsExtension,
                        IsStatic = m.IsStatic,
                        IsAsync = m.IsAsync
                    }),
                    TotalCount = membersList.Count,
                    FilteredCount = membersList.Count
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get available members");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Find where a C# symbol (class, method, property, etc.) is defined. Like 'Go to Definition' in Visual Studio.

                 Use this when:
                 - User asks 'Where is this method/class/property defined?'
                 - Need to jump to implementation to understand how something works
                 - Tracing through code flow to understand behavior
                 - Finding the source of a type to modify it
                 - Navigating from usage to declaration

                 Examples:
                 - Code: 'customer.SaveAsync()' -> Use at 'SaveAsync' to find where this method is implemented
                 - Code: 'IUserService service' -> Use at 'IUserService' to jump to interface definition
                 - Code: 'new ProductController()' -> Use at 'ProductController' to find the class definition
                 - Finding third-party definitions: Works even for external assemblies if source is available

                 Returns:
                 - DefinitionLocation: File path and exact line/column where defined
                 - SymbolInfo: Information about the symbol (name, kind, containing type)
                 - SourceText: Preview of the source code at the definition location
                 - Returns error if symbol is from external assembly without source
                 """)]
    public async Task<string> FindSymbolDefinition(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (location, symbolInfo, sourceText) = await roslynService.FindSymbolDefinitionAsync(
                filePath,
                line,
                column,
                solutionPath,
                cancellationToken);

            if (location == null || symbolInfo == null)
            {
                return new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Code = "SYMBOL_NOT_FOUND",
                        Message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new FindSymbolDefinitionResponse
            {
                Success = true,
                Data = new SymbolDefinitionData
                {
                    DefinitionLocation = new LocationData
                    {
                        FilePath = location.FilePath,
                        Line = location.Line,
                        Column = location.Column,
                        EndLine = location.EndLine,
                        EndColumn = location.EndColumn
                    },
                    SymbolInfo = new SymbolInfoData
                    {
                        Name = symbolInfo.SymbolName,
                        Kind = symbolInfo.Kind,
                        ContainingType = symbolInfo.FullTypeName
                    },
                    SourceText = sourceText
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find symbol definition");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Find all C# code locations that reference a symbol. Like 'Find All References' in Visual Studio.

                 Use this when:
                 - User asks 'Where is this method/class/property used?'
                 - Analyzing impact of changing a method signature
                 - Finding all callers of a method
                 - Refactoring: understanding what will be affected
                 - Removing dead code: checking if something is used

                 Examples:
                 - Method usage: Find all places calling 'SaveAsync()'
                 - Class usage: Find all instantiations and type references to 'UserService'
                 - Property usage: Find all reads/writes to 'Customer.Name'
                 - Interface usage: Find all places using 'IRepository<T>'

                 Parameters:
                 - includeDeclaration: false = only usages, true = include where it's defined
                 - maxResults: Limit results (default 100) for performance

                 Returns:
                 - References: Array of locations with:
                   - FilePath: Which file contains the reference
                   - Line/Column: Exact position
                   - LineText: Preview of the line of code
                   - Kind: Read, Write, Call, TypeReference, etc.
                 - TotalCount: Total number of references found
                 - HasMore: true if results were limited by maxResults
                 """)]
    public async Task<string> FindReferences(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include declaration")] bool includeDeclaration = false,
        [Description("Maximum results to return")] int maxResults = 100,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (references, totalCount, hasMore) = await roslynService.FindReferencesAsync(
                filePath,
                line,
                column,
                includeDeclaration,
                maxResults,
                solutionPath,
                cancellationToken);

            return new FindReferencesResponse
            {
                Success = true,
                Data = new ReferencesData
                {
                    References = references.Select(r => new ReferenceData
                    {
                        FilePath = r.FilePath,
                        Line = r.Line,
                        Column = r.Column,
                        LineText = r.LineText,
                        Kind = r.Kind
                    }),
                    TotalCount = totalCount,
                    HasMore = hasMore
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find references");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Find all C# types that implement an interface or derive from a base class. Essential for understanding polymorphism and inheritance.

                 Use this when:
                 - User asks 'What classes implement this interface?'
                 - Finding all concrete implementations of an abstraction
                 - Understanding inheritance hierarchy
                 - Locating all derived exception types
                 - Finding all controllers, services, or repositories in a pattern

                 Examples:
                 - Interface: 'IUserService' -> Finds UserService, MockUserService, CachedUserService
                 - Base class: 'Controller' -> Finds all MVC controllers in the solution
                 - Abstract class: 'BaseRepository' -> Finds all concrete repository implementations
                 - Exception: 'Exception' -> Finds all custom exception types

                 Parameters:
                 - Can search by position (cursor on interface) OR by typeName
                 - findDerivedTypes: Include classes that inherit
                 - findInterfaceImplementations: Include interface implementations

                 Returns:
                 - Implementations: Array of implementing types with:
                   - TypeName: Simple name (e.g., 'UserService')
                   - FullTypeName: Full name with namespace
                   - FilePath: Where the implementation is defined
                   - Line: Line number of the class declaration
                   - ImplementsDirectly: true if directly implements, false if through inheritance
                 """)]
    public async Task<string> FindImplementations(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Find derived types")] bool findDerivedTypes = true,
        [Description("Find interface implementations")] bool findInterfaceImplementations = true,
        [Description("Optional: specific type name to search for")] string? typeName = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var implementations = await roslynService.FindImplementationsAsync(
                filePath,
                line,
                column,
                findDerivedTypes,
                findInterfaceImplementations,
                typeName,
                solutionPath,
                cancellationToken);

            return new FindImplementationsResponse
            {
                Success = true,
                Data = new ImplementationsData
                {
                    Implementations = implementations.Select(i => new ImplementationData
                    {
                        TypeName = i.TypeName,
                        FullTypeName = i.FullTypeName,
                        FilePath = i.FilePath,
                        Line = i.Line,
                        Kind = i.Kind,
                        ImplementsDirectly = i.ImplementsDirectly
                    })
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find implementations");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Get complete C# type hierarchy showing inheritance and interface relationships. Visualizes the full inheritance chain.

                 Use this when:
                 - User asks 'What does this class inherit from?'
                 - Understanding the inheritance chain of a type
                 - Finding all base classes and interfaces
                 - Checking if a type implements specific interfaces
                 - Analyzing class design and architecture

                 Examples:
                 - Analyzing a service: 'UserService' -> Shows it inherits from BaseService, implements IUserService, IDisposable
                 - Understanding collections: 'List<T>' -> Shows IList<T>, ICollection<T>, IEnumerable<T> hierarchy
                 - Custom types: 'ProductController' -> Shows Controller -> ControllerBase -> object chain

                 Parameters:
                 - direction: 'Base' (what it inherits), 'Derived' (what inherits from it), 'Both'
                 - Can search by position OR by typeName

                 Returns:
                 - BaseTypes: Array of base classes in order (immediate parent first)
                 - DerivedTypes: Array of types that inherit from this type
                 - Interfaces: All interfaces implemented (including inherited ones)
                 - Each type includes: TypeName, FullTypeName, Assembly
                 """)]
    public async Task<string> GetTypeHierarchy(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Direction: Base, Derived, or Both")] string direction = "Both",
        [Description("Optional: specific type name to search for")] string? typeName = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hierarchy = await roslynService.GetTypeHierarchyAsync(
                filePath,
                line,
                column,
                direction,
                typeName,
                solutionPath,
                cancellationToken);

            return new GetTypeHierarchyResponse
            {
                Success = true,
                Data = new TypeHierarchyData
                {
                    BaseTypes = hierarchy.BaseTypes.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    }),
                    DerivedTypes = hierarchy.DerivedTypes.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    }),
                    Interfaces = hierarchy.Interfaces.Select(t => new TypeInfoData
                    {
                        TypeName = t.TypeName,
                        FullTypeName = t.FullTypeName,
                        Assembly = t.Assembly
                    })
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type hierarchy");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("""
                 Get all C# compiler errors and warnings for a file or entire solution. Essential for fixing build issues.

                 Use this when:
                 - User says 'Fix the build errors' or 'Why won't this compile?'
                 - Checking if code changes break the build
                 - Finding all warnings in a project
                 - Pre-commit validation
                 - Identifying deprecated API usage

                 Examples:
                 - Build errors: Get all CS errors preventing compilation
                 - Warnings: Find all nullable reference warnings, unused variable warnings
                 - Solution-wide: Omit filePath to check entire solution health
                 - Single file: Provide filePath to check just one file after editing

                 Returns:
                 - Diagnostics: All errors and warnings with full details
                 - Summary: Total count, error count, warning count
                 - Each diagnostic includes:
                   - Id: Diagnostic code
                   - Message: Full error description
                   - FilePath: Which file has the issue
                   - Line/Column: Exact location
                   - Severity: Error, Warning, Info
                 """)]
    public async Task<string> GetCompilationDiagnostics(
        [Description("Path to source file (optional - omit for entire solution)")] string? filePath = null,
        [Description("Override solution path (required if filePath is omitted)")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var diagnostics = await roslynService.GetCompilationDiagnosticsAsync(
                filePath,
                solutionPath,
                cancellationToken);

            var diagnosticsList = diagnostics.ToList();
            var errorCount = diagnosticsList.Count(d => d.Severity == "Error");
            var warningCount = diagnosticsList.Count(d => d.Severity == "Warning");

            return new GetCompilationDiagnosticsResponse
            {
                Success = true,
                Data = new CompilationDiagnosticsData
                {
                    Diagnostics = diagnosticsList.Select(d => new DiagnosticData
                    {
                        Id = d.Id,
                        Severity = d.Severity,
                        Message = d.Message,
                        FilePath = d.FilePath,
                        Line = d.Line,
                        Column = d.Column,
                        EndLine = d.EndLine,
                        EndColumn = d.EndColumn,
                        Category = d.Category
                    }),
                    Summary = new DiagnosticsSummary
                    {
                        TotalCount = diagnosticsList.Count,
                        ErrorCount = errorCount,
                        WarningCount = warningCount
                    }
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get compilation diagnostics");
            
            return new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Code = DetermineErrorCode(ex),
                    Message = ex.Message,
                    Details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    private string DetermineErrorCode(Exception ex)
    {
        return ex switch
        {
            InvalidOperationException => "SOLUTION_NOT_FOUND",
            FileNotFoundException => "FILE_NOT_FOUND",
            ArgumentException => "INVALID_POSITION",
            TaskCanceledException => "TIMEOUT",
            _ => "UNKNOWN_ERROR"
        };
    }
}