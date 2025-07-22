using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using RoslynMcp.Services;
using System.ComponentModel;
using RoslynMcp.Services.SerializerExtensions;

namespace RoslynMcp.Tools;

public class RoslynTools(
    RoslynService roslynService,
    ILogger<RoslynTools> logger)
{
    [McpServerTool]
    [Description("Initialize or detect the workspace and solution. Should be called once at the start of a session.")]
    public async Task<string> InitializeWorkspace(
        [Description("Directory to search from (defaults to current working directory)")] string? workingDirectory = null,
        [Description("Preferred solution if multiple are found")] string? preferredSolution = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await roslynService.InitializeWorkspaceAsync(workingDirectory, preferredSolution);

            
            return new
            {
                success = true,
                data = new
                {
                    primarySolution = workspace.PrimarySolution != null ? new
                    {
                        path = workspace.PrimarySolution.Path,
                        name = workspace.PrimarySolution.Name,
                        projectCount = workspace.PrimarySolution.ProjectCount
                    } : null,
                    allSolutions = workspace.AllSolutions.Select(s => new
                    {
                        path = s.Path,
                        projects = s.Projects
                    }),
                    frameworkVersions = workspace.FrameworkVersions,
                    workspaceRoot = workspace.WorkspaceRoot
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize workspace");
            
            return new
            {
                success = false,
                error = new
                {
                    code = "INITIALIZATION_FAILED",
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get type information at a specific position in the code.")]
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
                return new
                {
                    success = false,
                    error = new
                    {
                        code = "SYMBOL_NOT_FOUND",
                        message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new
            {
                success = true,
                data = new
                {
                    symbolName = symbolInfo.SymbolName,
                    fullTypeName = symbolInfo.FullTypeName,
                    kind = symbolInfo.Kind,
                    assembly = symbolInfo.Assembly,
                    @namespace = symbolInfo.Namespace,
                    documentation = symbolInfo.Documentation,
                    isGeneric = symbolInfo.IsGeneric,
                    typeArguments = symbolInfo.TypeArguments,
                    baseType = symbolInfo.BaseType,
                    interfaces = symbolInfo.Interfaces,
                    resolvedFromSolution = symbolInfo.ResolvedFromSolution
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get type at position");
            
            return new
            {
                success = false,
                error = new
                {
                    code = DetermineErrorCode(ex),
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Get all accessible members at a specific code position.")]
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

            return new
            {
                success = true,
                data = new
                {
                    members = membersList.Select(m => new
                    {
                        name = m.Name,
                        kind = m.Kind,
                        signature = m.Signature,
                        declaringType = m.DeclaringType,
                        accessibility = m.Accessibility,
                        documentation = m.Documentation,
                        parameters = m.Parameters?.Select(p => new
                        {
                            name = p.Name,
                            type = p.Type,
                            documentation = p.Documentation
                        }),
                        isExtension = m.IsExtension,
                        isStatic = m.IsStatic,
                        isAsync = m.IsAsync
                    }),
                    totalCount = membersList.Count,
                    filteredCount = membersList.Count
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get available members");
            
            return new
            {
                success = false,
                error = new
                {
                    code = DetermineErrorCode(ex),
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Find the definition location of a symbol.")]
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
                return new
                {
                    success = false,
                    error = new
                    {
                        code = "SYMBOL_NOT_FOUND",
                        message = $"No symbol found at position {line}:{column} in {filePath}"
                    }
                }.ToSerialized();
            }

            return new
            {
                success = true,
                data = new
                {
                    definitionLocation = new
                    {
                        filePath = location.FilePath,
                        line = location.Line,
                        column = location.Column,
                        endLine = location.EndLine,
                        endColumn = location.EndColumn
                    },
                    symbolInfo = new
                    {
                        name = symbolInfo.SymbolName,
                        kind = symbolInfo.Kind,
                        containingType = symbolInfo.FullTypeName
                    },
                    sourceText = sourceText
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find symbol definition");
            
            return new
            {
                success = false,
                error = new
                {
                    code = DetermineErrorCode(ex),
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
                }
            }.ToSerialized();
        }
    }

    [McpServerTool]
    [Description("Find all references to a symbol.")]
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

            return new
            {
                success = true,
                data = new
                {
                    references = references.Select(r => new
                    {
                        filePath = r.FilePath,
                        line = r.Line,
                        column = r.Column,
                        lineText = r.LineText,
                        kind = r.Kind
                    }),
                    totalCount = totalCount,
                    hasMore = hasMore
                }
            }.ToSerialized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to find references");
            
            return new
            {
                success = false,
                error = new
                {
                    code = DetermineErrorCode(ex),
                    message = ex.Message,
                    details = new { exception = ex.GetType().Name }
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