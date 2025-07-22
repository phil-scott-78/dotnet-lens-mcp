using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using RoslynMcp.Models;

namespace RoslynMcp.Services;

public class RoslynService
{
    private readonly SolutionCache _solutionCache;
    private readonly WorkspaceResolver _workspaceResolver;
    private readonly ILogger<RoslynService> _logger;

    public RoslynService(
        SolutionCache solutionCache,
        WorkspaceResolver workspaceResolver,
        ILogger<RoslynService> logger)
    {
        _solutionCache = solutionCache;
        _workspaceResolver = workspaceResolver;
        _logger = logger;
    }

    public async Task<WorkspaceInfo> InitializeWorkspaceAsync(
        string? workingDirectory = null, 
        string? preferredSolution = null)
    {
        return await _workspaceResolver.InitializeWorkspaceAsync(workingDirectory, preferredSolution);
    }

    public async Task<string?> ResolveEffectiveSolutionPath(
        string filePath, 
        string? explicitSolutionPath = null)
    {
        if (!string.IsNullOrEmpty(explicitSolutionPath))
        {
            return Path.GetFullPath(explicitSolutionPath);
        }

        var solution = await _workspaceResolver.FindSolutionForFileAsync(filePath);
        if (solution == null)
        {
            _logger.LogWarning("Could not find solution for file: {FilePath}", filePath);
        }
        
        return solution;
    }

    public async Task<Models.SymbolInfo?> GetTypeAtPositionAsync(
        string filePath, 
        int line, 
        int column, 
        string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveSolutionPath = await ResolveEffectiveSolutionPath(filePath, solutionPath);
        if (string.IsNullOrEmpty(effectiveSolutionPath))
        {
            throw new InvalidOperationException($"Could not find solution for file: {filePath}");
        }

        var solution = await _solutionCache.GetSolutionAsync(effectiveSolutionPath, cancellationToken);
        var document = GetDocument(solution, filePath);
        
        if (document == null)
        {
            _logger.LogWarning("Document not found in solution: {FilePath}", filePath);
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            _logger.LogWarning("Could not get semantic model for document: {FilePath}", filePath);
            return null;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return null;
        }

        var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
        var token = root.FindToken(position);
        
        if (token.IsKind(SyntaxKind.None))
        {
            return null;
        }

        var node = token.Parent;
        ISymbol? symbol = null;

        while (node != null && symbol == null)
        {
            symbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol ?? 
                    semanticModel.GetDeclaredSymbol(node, cancellationToken);
            
            if (symbol == null)
            {
                node = node.Parent;
            }
        }

        if (symbol == null)
        {
            var typeInfo = semanticModel.GetTypeInfo(token.Parent!, cancellationToken);
            if (typeInfo.Type != null)
            {
                symbol = typeInfo.Type;
            }
        }

        if (symbol == null)
        {
            return null;
        }

        return CreateSymbolInfo(symbol, effectiveSolutionPath);
    }

    private Document? GetDocument(Solution solution, string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath != null && 
                    Path.GetFullPath(document.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
        }

        return null;
    }

    private int GetPosition(Microsoft.CodeAnalysis.Text.SourceText text, int line, int column)
    {
        var linePosition = new Microsoft.CodeAnalysis.Text.LinePosition(line - 1, column - 1);
        return text.Lines.GetPosition(linePosition);
    }

    private Models.SymbolInfo CreateSymbolInfo(ISymbol symbol, string solutionPath)
    {
        var type = symbol as ITypeSymbol ?? (symbol as IPropertySymbol)?.Type ?? (symbol as IFieldSymbol)?.Type;
        
        var info = new Models.SymbolInfo
        {
            SymbolName = symbol.Name,
            FullTypeName = symbol.ToDisplayString(),
            Kind = symbol.Kind.ToString(),
            Assembly = symbol.ContainingAssembly?.Name,
            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
            Documentation = GetDocumentation(symbol),
            ResolvedFromSolution = solutionPath
        };

        if (type != null)
        {
            info.FullTypeName = type.ToDisplayString();
            info.IsGeneric = type is INamedTypeSymbol { IsGenericType: true };
            
            if (type is INamedTypeSymbol namedType)
            {
                info.BaseType = namedType.BaseType?.ToDisplayString();
                info.Interfaces = namedType.Interfaces.Select(i => i.ToDisplayString()).ToList();
                
                if (namedType.IsGenericType)
                {
                    info.TypeArguments = namedType.TypeArguments.Select(t => t.ToDisplayString()).ToList();
                }
            }
        }

        return info;
    }

    private string? GetDocumentation(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var summary = doc.Descendants("summary").FirstOrDefault();
            if (summary != null)
            {
                return summary.Value.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse XML documentation");
        }

        return null;
    }
}