using System.ComponentModel;
using DotnetLensMcp.Models.Responses;
using DotnetLensMcp.SerializerExtensions;
using DotnetLensMcp.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DotnetLensMcp.Tools;

public class RoslynTools(
    RoslynService roslynService)
{
    [McpServerTool] 
    [Description("Initialize C# or .NET workspace and solution detection. MUST be called first before using any other C# or .NET tools. Use when starting analysis of any C# or .NET project, working with .csproj, .sln, or .cs files, or before performing any code analysis operations.")]
    public async Task<InitializeWorkspaceResponse> InitializeWorkspace(
        [Description("Directory to search from (defaults to current working directory)")] string? workingDirectory = null,
        [Description("Preferred solution if multiple are found")] string? preferredSolution = null,
        CancellationToken cancellationToken = default)
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
        };
    }

    [McpServerTool]
    [Description("Get C# type information at a specific cursor position. Resolves the type of variables, expressions, method calls, etc. Use when analyzing types, understanding var keywords, checking return types, or hovering over symbols.")]
    public async Task<GetTypeAtPositionResponse> GetTypeAtPosition(
        [Description("Path to source file (absolute or relative to workspace)")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path (uses initialized workspace if not provided)")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var symbolInfo = await roslynService.GetTypeAtPositionAsync(
            filePath, 
            line, 
            column, 
            solutionPath, 
            cancellationToken);

        if (symbolInfo == null)
        {
            throw new InvalidOperationException($"No symbol found at position {line}:{column} in {filePath}");
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
        };
    }

    [McpServerTool]
    [Description("Get all available C# members (methods, properties, fields) accessible at a specific code position. Like IntelliSense autocomplete. Use when exploring APIs, checking available methods, or finding extension methods.")]
    public async Task<GetAvailableMembersResponse> GetAvailableMembers(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include extension methods")] bool includeExtensionMethods = true,
        [Description("Include static members")] bool includeStatic = true,
        [Description("Filter by name prefix")] string? filter = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
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
        };
    }

    [McpServerTool]
    [Description("Find where a C# symbol (class, method, property, etc.) is defined. Like 'Go to Definition' in Visual Studio. Use when locating symbol definitions, jumping to implementations, or navigating from usage to declaration.")]
    public async Task<FindSymbolDefinitionResponse> FindSymbolDefinition(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var (location, symbolInfo, sourceText) = await roslynService.FindSymbolDefinitionAsync(
            filePath,
            line,
            column,
            solutionPath,
            cancellationToken);

        if (location == null || symbolInfo == null)
        {
            throw new InvalidOperationException($"No symbol found at position {line}:{column} in {filePath}");
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
        };
    }

    [McpServerTool]
    [Description("Find all C# code locations that reference a symbol. Like 'Find All References' in Visual Studio. Use when analyzing symbol usage, finding callers, assessing refactoring impact, or identifying dead code.")]
    public async Task<FindReferencesResponse> FindReferences(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Include declaration")] bool includeDeclaration = false,
        [Description("Maximum results to return")] int maxResults = 100,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
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
        };
    }

    [McpServerTool]
    [Description("Find all C# types that implement an interface or derive from a base class. Essential for understanding polymorphism and inheritance. Use when finding implementations, exploring inheritance hierarchies, or locating derived types.")]
    public async Task<FindImplementationsResponse> FindImplementations(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Find derived types")] bool findDerivedTypes = true,
        [Description("Find interface implementations")] bool findInterfaceImplementations = true,
        [Description("Optional: specific type name to search for")] string? typeName = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
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
        };
    }

    [McpServerTool]
    [Description("Get complete C# type hierarchy showing inheritance and interface relationships. Visualizes the full inheritance chain. Use when analyzing class inheritance, finding base classes and interfaces, or understanding type architecture.")]
    public async Task<GetTypeHierarchyResponse> GetTypeHierarchy(
        [Description("Path to source file")] string filePath,
        [Description("1-based line number")] int line,
        [Description("1-based column number")] int column,
        [Description("Direction: Base, Derived, or Both")] string direction = "Both",
        [Description("Optional: specific type name to search for")] string? typeName = null,
        [Description("Override solution path")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
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
        };
    }

    [McpServerTool]
    [Description("Get all C# compiler errors and warnings for a file or entire solution. Essential for fixing build issues. Use when debugging compilation errors, checking build status, finding warnings, or validating code changes.")]
    public async Task<GetCompilationDiagnosticsResponse> GetCompilationDiagnostics(
        [Description("Path to source file (optional - omit for entire solution)")] string? filePath = null,
        [Description("Override solution path (required if filePath is omitted)")] string? solutionPath = null,
        CancellationToken cancellationToken = default)
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
        };
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