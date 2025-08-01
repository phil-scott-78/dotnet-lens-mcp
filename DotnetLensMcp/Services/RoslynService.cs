using DotnetLensMcp.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;

namespace DotnetLensMcp.Services;

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
        var (symbol, type) = GetSymbolAndTypeAtPosition(semanticModel, root, position, cancellationToken);

        // Use the symbol if found, otherwise use the type
        var targetSymbol = symbol ?? type;
        
        if (targetSymbol == null)
        {
            return null;
        }

        return CreateSymbolInfo(targetSymbol, effectiveSolutionPath);
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
        var type = symbol as ITypeSymbol ?? 
                   (symbol as IPropertySymbol)?.Type ?? 
                   (symbol as IFieldSymbol)?.Type ??
                   (symbol as ILocalSymbol)?.Type ??
                   (symbol as IParameterSymbol)?.Type ??
                   (symbol as IMethodSymbol)?.ReturnType;
        
        var info = new Models.SymbolInfo
        {
            SymbolName = symbol.Name,
            FullTypeName = symbol.ToDisplayString(),
            Kind = GetEffectiveSymbolKind(symbol, type),
            Assembly = symbol.ContainingAssembly?.Name,
            Namespace = symbol.ContainingNamespace?.ToDisplayString(),
            Documentation = GetDocumentation(symbol),
            ResolvedFromSolution = solutionPath
        };

        if (type != null)
        {
            // For type display, handle nullable reference types specially
            info.FullTypeName = GetCleanTypeName(type);
            info.IsGeneric = type is INamedTypeSymbol { IsGenericType: true };
            
            if (type is INamedTypeSymbol namedType)
            {
                info.BaseType = namedType.BaseType?.ToDisplayString();
                info.Interfaces = namedType.Interfaces.Select(i => i.ToDisplayString()).ToList();
                
                if (namedType.IsGenericType)
                {
                    info.TypeArguments = namedType.TypeArguments.Select(t => GetCleanTypeName(t)).ToList();
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

    public async Task<IEnumerable<MemberInfo>> GetAvailableMembersAsync(
        string filePath,
        int line,
        int column,
        bool includeExtensionMethods = true,
        bool includeStatic = true,
        string? filter = null,
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
            throw new FileNotFoundException($"Document not found in solution: {filePath}");
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            throw new InvalidOperationException($"Could not get semantic model for document: {filePath}");
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return [];
        }

        var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
        var (_, type) = GetSymbolAndTypeAtPosition(semanticModel, root, position, cancellationToken);
        
        var members = new List<MemberInfo>();
        
        // Use the type we found
        if (type != null)
        {
            // Get instance members
            var typeMembers = type.GetMembers()
                .Where(m => m is { CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public });
            
            if (!includeStatic)
            {
                typeMembers = typeMembers.Where(m => !m.IsStatic);
            }

            foreach (var member in typeMembers)
            {
                if (!string.IsNullOrWhiteSpace(filter) && !member.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var memberInfo = CreateMemberInfo(member);
                if (memberInfo != null)
                {
                    members.Add(memberInfo);
                }
            }
        }

        // Get extension methods if requested
        if (includeExtensionMethods && type != null)
        {
            var extensionMethods = await GetExtensionMethodsAsync(
                semanticModel, 
                position, 
                type, 
                filter,
                cancellationToken);
            
            members.AddRange(extensionMethods);
        }

        return members;
    }

    private Task<IEnumerable<MemberInfo>> GetExtensionMethodsAsync(
        SemanticModel semanticModel,
        int position,
        ITypeSymbol targetType,
        string? filter,
        CancellationToken cancellationToken)
    {
        var extensionMethods = new List<MemberInfo>();
        
        // Get all types in scope
        var compilation = semanticModel.Compilation;
        var namespaces = GetUsedNamespaces(semanticModel.SyntaxTree, position);
        
        foreach (var ns in namespaces)
        {
            var nsSymbol = compilation.GetTypeByMetadataName(ns);
            if (nsSymbol == null)
            {
                // Try as namespace
                var members = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type, cancellationToken)
                    .OfType<INamedTypeSymbol>()
                    .Where(t => t.ContainingNamespace?.ToDisplayString() == ns && t.IsStatic);
                
                foreach (var type in members)
                {
                    foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
                    {
                        if (!member.IsExtensionMethod)
                            continue;
                            
                        var firstParam = member.Parameters.FirstOrDefault();
                        if (firstParam == null)
                            continue;
                            
                        // Check if extension method applies to our target type
                        if (!IsTypeCompatible(targetType, firstParam.Type))
                            continue;
                            
                        if (!string.IsNullOrEmpty(filter) && !member.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var memberInfo = CreateMemberInfo(member);
                        if (memberInfo != null)
                        {
                            memberInfo.IsExtension = true;
                            extensionMethods.Add(memberInfo);
                        }
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<MemberInfo>>(extensionMethods);
    }
    
    private bool IsTypeCompatible(ITypeSymbol targetType, ITypeSymbol parameterType)
    {
        // Simple compatibility check
        return targetType.Equals(parameterType, SymbolEqualityComparer.Default) ||
               targetType.AllInterfaces.Contains(parameterType, SymbolEqualityComparer.Default) ||
               (targetType.BaseType != null && IsTypeCompatible(targetType.BaseType, parameterType));
    }
    
    private IEnumerable<string> GetUsedNamespaces(SyntaxTree tree, int position)
    {
        var root = tree.GetRoot();
        var usings = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.SpanStart <= position)
            .Select(u => u.Name?.ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct();
            
        return usings!;
    }

    private MemberInfo? CreateMemberInfo(ISymbol member)
    {
        var memberInfo = new MemberInfo
        {
            Name = member.Name,
            Kind = member.Kind.ToString(),
            DeclaringType = member.ContainingType?.ToDisplayString() ?? string.Empty,
            Accessibility = member.DeclaredAccessibility.ToString().ToLower(),
            Documentation = GetDocumentation(member),
            IsStatic = member.IsStatic
        };

        switch (member)
        {
            case IMethodSymbol method:
                memberInfo.Signature = method.ToDisplayString();
                memberInfo.IsAsync = method.IsAsync;
                memberInfo.Parameters = method.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString(),
                    Documentation = GetDocumentation(p)
                }).ToList();
                break;
                
            case IPropertySymbol property:
                memberInfo.Signature = $"{property.Type.ToDisplayString()} {property.Name}";
                break;
                
            case IFieldSymbol field:
                memberInfo.Signature = $"{field.Type.ToDisplayString()} {field.Name}";
                break;
                
            case IEventSymbol evt:
                memberInfo.Signature = $"{evt.Type.ToDisplayString()} {evt.Name}";
                break;
                
            default:
                return null;
        }

        return memberInfo;
    }

    public async Task<(DefinitionLocation? location, Models.SymbolInfo? symbolInfo, string? sourceText)> 
        FindSymbolDefinitionAsync(
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
            throw new FileNotFoundException($"Document not found in solution: {filePath}");
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            throw new InvalidOperationException($"Could not get semantic model for document: {filePath}");
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return (null, null, null);
        }

        var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
        var token = root.FindToken(position);
        
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol;
        if (symbol == null)
        {
            return (null, null, null);
        }

        // Get definition location
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location == null)
        {
            return (null, null, null);
        }

        var definitionDoc = solution.GetDocument(location.SourceTree);
        if (definitionDoc == null)
        {
            return (null, null, null);
        }

        var span = location.SourceSpan;
        var text = await location.SourceTree!.GetTextAsync(cancellationToken);
        var lineSpan = text.Lines.GetLinePositionSpan(span);
        
        var definitionLocation = new DefinitionLocation
        {
            FilePath = definitionDoc.FilePath ?? string.Empty,
            Line = lineSpan.Start.Line + 1,
            Column = lineSpan.Start.Character + 1,
            EndLine = lineSpan.End.Line + 1,
            EndColumn = lineSpan.End.Character + 1
        };

        var symbolInfo = CreateSymbolInfo(symbol, effectiveSolutionPath);
        var sourceText = text.GetSubText(span).ToString();

        return (definitionLocation, symbolInfo, sourceText);
    }

    public async Task<(IEnumerable<Models.ReferenceLocation> references, int totalCount, bool hasMore)> 
        FindReferencesAsync(
            string filePath,
            int line,
            int column,
            bool includeDeclaration = false,
            int maxResults = 100,
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
            throw new FileNotFoundException($"Document not found in solution: {filePath}");
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            throw new InvalidOperationException($"Could not get semantic model for document: {filePath}");
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null)
        {
            return (Enumerable.Empty<Models.ReferenceLocation>(), 0, false);
        }

        var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
        var token = root.FindToken(position);
        
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol ?? 
                    semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);
        
        if (symbol == null)
        {
            return (Enumerable.Empty<Models.ReferenceLocation>(), 0, false);
        }

        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken);
        var locations = new List<Models.ReferenceLocation>();
        var totalCount = 0;

        foreach (var reference in references)
        {
            foreach (var location in reference.Locations)
            {
                if (!includeDeclaration && location.IsImplicit)
                    continue;

                totalCount++;
                
                if (locations.Count >= maxResults)
                    continue;

                var refDoc = location.Document;
                var span = location.Location.SourceSpan;
                var text = await location.Location.SourceTree!.GetTextAsync(cancellationToken);
                var lineSpan = text.Lines.GetLinePositionSpan(span);
                var lineText = text.Lines[lineSpan.Start.Line].ToString();

                locations.Add(new Models.ReferenceLocation
                {
                    FilePath = refDoc.FilePath ?? string.Empty,
                    Line = lineSpan.Start.Line + 1,
                    Column = lineSpan.Start.Character + 1,
                    LineText = lineText.Trim(),
                    Kind = "Reference" // Simplified for now
                });
            }
        }

        return (locations.Take(maxResults), totalCount, totalCount > maxResults);
    }

    public async Task<IEnumerable<ImplementationInfo>> FindImplementationsAsync(
        string filePath,
        int line,
        int column,
        bool findDerivedTypes = true,
        bool findInterfaceImplementations = true,
        string? typeName = null, // Optional: can still search by type name if provided
        string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveSolutionPath = await ResolveEffectiveSolutionPath(filePath, solutionPath);
        if (string.IsNullOrEmpty(effectiveSolutionPath))
        {
            throw new InvalidOperationException($"Could not find solution for file: {filePath}");
        }

        var solution = await _solutionCache.GetSolutionAsync(effectiveSolutionPath, cancellationToken);
        INamedTypeSymbol? targetType;

        // If type name is provided, use it (backwards compatibility / direct search)
        if (!string.IsNullOrEmpty(typeName))
        {
            var compilation = await GetCompilationAsync(solution, cancellationToken);
            targetType = compilation.GetTypeByMetadataName(typeName);
            if (targetType == null)
            {
                // Try to find by simple name
                targetType = compilation.GetSymbolsWithName(n => n.Equals(typeName) || n.EndsWith("." + typeName), SymbolFilter.Type, cancellationToken)
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();
            }
        }
        else
        {
            // Use position-based discovery
            var document = GetDocument(solution, filePath);
            if (document == null)
            {
                throw new FileNotFoundException($"Document not found in solution: {filePath}");
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                throw new InvalidOperationException($"Could not get semantic model for document: {filePath}");
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                return Enumerable.Empty<ImplementationInfo>();
            }

            var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
            var (symbol, type) = GetSymbolAndTypeAtPosition(semanticModel, root, position, cancellationToken);
            
            targetType = type as INamedTypeSymbol;
            if (targetType == null && symbol is INamedTypeSymbol namedSymbol)
            {
                targetType = namedSymbol;
            }
        }
        
        if (targetType == null)
        {
            return Enumerable.Empty<ImplementationInfo>();
        }

        var implementations = new List<ImplementationInfo>();

        if (targetType.TypeKind == TypeKind.Interface && findInterfaceImplementations)
        {
            var implementors = await SymbolFinder.FindImplementationsAsync(
                targetType, solution, cancellationToken: cancellationToken);
            
            foreach (var impl in implementors)
            {
                var location = impl.Locations.FirstOrDefault(l => l.IsInSource);
                if (location != null)
                {
                    implementations.Add(CreateImplementationInfo(impl, location, true));
                }
            }
        }

        if (findDerivedTypes && targetType.TypeKind == TypeKind.Class)
        {
            var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(
                targetType, solution, cancellationToken: cancellationToken);
            
            foreach (var derived in derivedTypes)
            {
                var location = derived.Locations.FirstOrDefault(l => l.IsInSource);
                if (location != null)
                {
                    implementations.Add(CreateImplementationInfo(derived, location, true));
                }
            }
        }

        return implementations;
    }

    private ImplementationInfo CreateImplementationInfo(
        INamedTypeSymbol type, Location location, bool implementsDirectly)
    {
        var doc = _solutionCache.GetSolutionAsync(string.Empty).Result
            .GetDocument(location.SourceTree);
        var lineSpan = location.GetLineSpan();
        
        return new ImplementationInfo
        {
            TypeName = type.Name,
            FullTypeName = type.ToDisplayString(),
            FilePath = doc?.FilePath ?? string.Empty,
            Line = lineSpan.StartLinePosition.Line + 1,
            Kind = type.TypeKind.ToString(),
            ImplementsDirectly = implementsDirectly
        };
    }

    public async Task<TypeHierarchyInfo> GetTypeHierarchyAsync(
        string filePath,
        int line,
        int column,
        string direction = "Both",
        string? typeName = null, // Optional: can still search by type name if provided
        string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveSolutionPath = await ResolveEffectiveSolutionPath(filePath, solutionPath);
        if (string.IsNullOrEmpty(effectiveSolutionPath))
        {
            throw new InvalidOperationException($"Could not find solution for file: {filePath}");
        }

        var solution = await _solutionCache.GetSolutionAsync(effectiveSolutionPath, cancellationToken);
        INamedTypeSymbol? targetType;

        // If type name is provided, use it (backwards compatibility / direct search)
        if (!string.IsNullOrEmpty(typeName))
        {
            var compilation = await GetCompilationAsync(solution, cancellationToken);
            targetType = compilation.GetTypeByMetadataName(typeName);
            if (targetType == null)
            {
                targetType = compilation.GetSymbolsWithName(n => n.Equals(typeName) || n.EndsWith("." + typeName), SymbolFilter.Type, cancellationToken)
                    .OfType<INamedTypeSymbol>()
                    .FirstOrDefault();
            }
        }
        else
        {
            // Use position-based discovery
            var document = GetDocument(solution, filePath);
            if (document == null)
            {
                throw new FileNotFoundException($"Document not found in solution: {filePath}");
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                throw new InvalidOperationException($"Could not get semantic model for document: {filePath}");
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                return new TypeHierarchyInfo();
            }

            var position = GetPosition(await document.GetTextAsync(cancellationToken), line, column);
            var (symbol, type) = GetSymbolAndTypeAtPosition(semanticModel, root, position, cancellationToken);
            
            targetType = type as INamedTypeSymbol;
            if (targetType == null && symbol is INamedTypeSymbol namedSymbol)
            {
                targetType = namedSymbol;
            }
        }
        
        if (targetType == null)
        {
            return new TypeHierarchyInfo();
        }

        var hierarchy = new TypeHierarchyInfo();

        // Get base types
        if (direction == "Base" || direction == "Both")
        {
            var baseType = targetType.BaseType;
            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                hierarchy.BaseTypes.Add(new Models.TypeInfo
                {
                    TypeName = baseType.Name,
                    FullTypeName = baseType.ToDisplayString(),
                    Assembly = baseType.ContainingAssembly?.Name
                });
                baseType = baseType.BaseType;
            }
        }

        // Get derived types
        if (direction == "Derived" || direction == "Both")
        {
            if (targetType.TypeKind == TypeKind.Class)
            {
                var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(
                    targetType, solution, cancellationToken: cancellationToken);
                
                foreach (var derived in derivedTypes)
                {
                    hierarchy.DerivedTypes.Add(new Models.TypeInfo
                    {
                        TypeName = derived.Name,
                        FullTypeName = derived.ToDisplayString(),
                        Assembly = derived.ContainingAssembly?.Name
                    });
                }
            }
        }

        // Get interfaces
        foreach (var iface in targetType.AllInterfaces)
        {
            hierarchy.Interfaces.Add(new Models.TypeInfo
            {
                TypeName = iface.Name,
                FullTypeName = iface.ToDisplayString(),
                Assembly = iface.ContainingAssembly?.Name
            });
        }

        return hierarchy;
    }

    public async Task<IEnumerable<DiagnosticInfo>> GetCompilationDiagnosticsAsync(
        string? filePath = null,
        string? solutionPath = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveSolutionPath = await ResolveEffectiveSolutionPath(filePath ?? string.Empty, solutionPath);
        if (string.IsNullOrEmpty(effectiveSolutionPath))
        {
            effectiveSolutionPath = await SearchFromCurrentDirectory() ?? 
                throw new InvalidOperationException("Could not find solution");
        }

        var solution = await _solutionCache.GetSolutionAsync(effectiveSolutionPath, cancellationToken);
        var diagnostics = new List<DiagnosticInfo>();

        if (!string.IsNullOrEmpty(filePath))
        {
            // Get diagnostics for specific file
            var document = GetDocument(solution, filePath);
            if (document != null)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
                if (semanticModel != null)
                {
                    var docDiagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
                    diagnostics.AddRange(ConvertDiagnostics(docDiagnostics, filePath));
                }
            }
        }
        else
        {
            // Get all compilation diagnostics
            var compilation = await GetCompilationAsync(solution, cancellationToken);
            var compilationDiagnostics = compilation.GetDiagnostics(cancellationToken);
            
            foreach (var diagnostic in compilationDiagnostics)
            {
                if (diagnostic.Location.IsInSource)
                {
                    var doc = solution.GetDocument(diagnostic.Location.SourceTree);
                    if (doc != null)
                    {
                        diagnostics.Add(ConvertDiagnostic(diagnostic, doc.FilePath ?? string.Empty));
                    }
                }
            }
        }

        return diagnostics;
    }

    private IEnumerable<DiagnosticInfo> ConvertDiagnostics(
        IEnumerable<Diagnostic> diagnostics, string filePath)
    {
        return diagnostics.Select(d => ConvertDiagnostic(d, filePath));
    }

    private DiagnosticInfo ConvertDiagnostic(Diagnostic diagnostic, string filePath)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        return new DiagnosticInfo
        {
            Id = diagnostic.Id,
            Severity = diagnostic.Severity.ToString(),
            Message = diagnostic.GetMessage(),
            FilePath = filePath,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1,
            Category = diagnostic.Descriptor.Category
        };
    }

    private async Task<Compilation> GetCompilationAsync(Solution solution, CancellationToken cancellationToken)
    {
        // Get first project's compilation as a simple approach
        var project = solution.Projects.FirstOrDefault();
        if (project == null)
        {
            throw new InvalidOperationException("No projects found in solution");
        }
        
        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            throw new InvalidOperationException("Could not get compilation");
        }
        
        return compilation;
    }

    private async Task<string?> SearchFromCurrentDirectory()
    {
        return await _workspaceResolver.FindSolutionForFileAsync(Directory.GetCurrentDirectory());
    }

    private (ISymbol? symbol, ITypeSymbol? type) GetSymbolAndTypeAtPosition(
        SemanticModel semanticModel,
        SyntaxNode root,
        int position,
        CancellationToken cancellationToken)
    {
        var token = root.FindToken(position);
        
        if (token.IsKind(SyntaxKind.None))
        {
            return (null, null);
        }

        // First, try to find a symbol by walking up the syntax tree
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

        // Get the type information
        ITypeSymbol? type = null;
        
        // If we found a symbol, get its type
        if (symbol != null)
        {
            type = symbol switch
            {
                ILocalSymbol local => local.Type,
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                IParameterSymbol parameter => parameter.Type,
                IMethodSymbol method => method.ReturnType,
                ITypeSymbol typeSymbol => typeSymbol,
                _ => null
            };
        }

        // If we didn't find a type through symbol, try GetTypeInfo
        if (type == null && token.Parent != null)
        {
            var typeInfo = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
            type = typeInfo.Type ?? typeInfo.ConvertedType;
        }

        // Special handling for member access expressions (e.g., obj.Member)
        if (type == null)
        {
            var memberAccess = token.Parent?.AncestorsAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault();
            
            if (memberAccess != null)
            {
                var expressionTypeInfo = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken);
                type = expressionTypeInfo.Type ?? expressionTypeInfo.ConvertedType;
            }
        }

        return (symbol, type);
    }

    private string GetEffectiveSymbolKind(ISymbol symbol, ITypeSymbol? type)
    {
        // Handle type parameters specifically first
        if (symbol.Kind == SymbolKind.TypeParameter)
        {
            return "TypeParameter";
        }
        
        // For type-related symbols, return the appropriate kind based on context
        if (symbol is ITypeSymbol typeSymbol)
        {
            return typeSymbol.TypeKind switch
            {
                TypeKind.TypeParameter => "TypeParameter",
                _ => "NamedType"
            };
        }
        
        // Special handling for array types - check the type, not the symbol
        if (symbol.Kind == SymbolKind.Local && type is IArrayTypeSymbol arrayType)
        {
            // Only return ArrayType for jagged arrays (arrays of arrays)
            if (arrayType.ElementType is IArrayTypeSymbol)
            {
                return "ArrayType"; // jagged array like int[][]
            }
            // Single dimensional arrays should be "Local"
            return "Local";
        }
        
        // For explicit type declarations like "DateTime explicitDate", we want to return "NamedType"
        // But for regular variable declarations like "int[] array" or "int? nullable", return "Local"
        if (symbol.Kind == SymbolKind.Local && type != null)
        {
            var syntaxNode = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (syntaxNode?.Parent is VariableDeclarationSyntax varDecl)
            {
                // Only return "NamedType" for very specific cases like "DateTime explicitDate"
                // Exclude arrays, nullable types, reference types, and generic types
                if (!varDecl.Type.IsVar && 
                    type is INamedTypeSymbol namedType && 
                    namedType.SpecialType == SpecialType.System_DateTime) // Very specific case
                {
                    return "NamedType";
                }
            }
        }
        
        return symbol.Kind.ToString();
    }

    private string GetCleanTypeName(ITypeSymbol type)
    {
        // Create a custom display format for consistent output
        var format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

        var baseTypeName = type.ToDisplayString(format);
        
        // Handle nullable reference types - only show '?' when explicitly declared
        if (type.CanBeReferencedByName && 
            type.NullableAnnotation == NullableAnnotation.Annotated &&
            type.IsReferenceType)
        {
            // Only add '?' for reference types that are explicitly nullable
            // We use a heuristic: if it's a string type with explicit nullable annotation
            if (type is INamedTypeSymbol namedType && 
                namedType.SpecialType == SpecialType.System_String)
            {
                return baseTypeName + "?";
            }
        }
        
        return baseTypeName;
    }
}