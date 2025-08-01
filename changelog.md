# Roslyn MCP Server Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Phase 4 features (planned)

---

## [0.3.0] - 2025-07-22 - Phase 3 Completion

### Added
- `FindImplementations` tool for finding interface implementations and derived types
- `GetTypeHierarchy` tool for exploring inheritance hierarchies
- `AnalyzeCodeBlock` tool with code analysis and complexity metrics
- `GetCompilationDiagnostics` tool for retrieving compilation errors and warnings
- Support for both file-specific and solution-wide diagnostics
- Cyclomatic complexity calculation for code blocks
- Symbol discovery within code blocks (declared and referenced symbols)
- Type hierarchy exploration in both directions (base types and derived types)
- Interface implementation discovery

### Improved
- Better type resolution using compilation symbols
- Enhanced diagnostic information with categories
- Support for finding types by simple name or fully qualified name
- Comprehensive code block analysis including metrics

### Technical Details
- Implemented models for ImplementationInfo, TypeHierarchyInfo, DiagnosticInfo, and CodeBlockAnalysis
- Added compilation-level analysis capabilities
- Integrated SymbolFinder for finding implementations and derived types
- Added cyclomatic complexity calculation based on control flow constructs
- Fixed API compatibility issues with GetDiagnostics and Category property access
- Improved error handling for missing solutions or types

---

## [0.2.0] - 2025-07-22 - Phase 2 Completion

### Added
- `GetAvailableMembers` tool with filtering and extension method support
- `FindSymbolDefinition` tool for go-to-definition functionality  
- `FindReferences` tool for finding all usages
- Extension methods discovery in GetAvailableMembersAsync
- Support for filtering members by name prefix
- Support for including/excluding static members
- Comprehensive member information including parameters and documentation
- Reference location tracking with line text and reference kind
- Symbol definition location with source text extraction

### Improved
- Error handling with standardized error codes across all tools
- File system watching already implemented in SolutionCache
- Member discovery includes proper signatures and parameter information
- Extension method discovery using compilation symbols

### Technical Details
- Implemented custom extension method discovery using compilation symbols
- Added models for DefinitionLocation and ReferenceLocation
- Enhanced MemberInfo with ParameterInfo for detailed method signatures
- Improved type compatibility checking for extension methods
- Added namespace resolution from using directives
- Simplified reference kind detection due to API limitations

---

## [0.1.0] - 2025-07-22 - Phase 1 Completion

### Added
- Initial MCP server implementation with stdio communication
- Core project structure with Models, Services, and Tools
- Core models: SymbolInfo, MemberInfo, LocationInfo, WorkspaceInfo
- Solution caching system with 30-minute expiration and file watching
- Workspace resolver for automatic solution discovery
- RoslynService base class with solution loading capabilities
- `InitializeWorkspace` tool for workspace setup and solution discovery
- `GetTypeAtPosition` tool for type information retrieval at cursor positions
- Basic error handling with standardized error codes (SOLUTION_NOT_FOUND, FILE_NOT_FOUND, etc.)
- Support for both .sln and .csproj file loading
- Automatic MSBuild initialization via MSBuildLocator

### Technical Details
- Roslyn Workspaces API integration with Microsoft.CodeAnalysis
- ConcurrentDictionary-based solution cache with LRU-style expiration
- Automatic solution discovery from file paths (upward directory traversal)
- FileSystemWatcher integration for cache invalidation
- Proper async/await patterns with CancellationToken support
- Structured logging with Microsoft.Extensions.Logging
- ModelContextProtocol.Server integration using McpServerTool attributes

---

## [0.2.0] - Phase 2 Completion (Planned)

### Added
- `get_available_members` tool with filtering and extension method support
- `find_symbol_definition` tool for go-to-definition functionality
- `find_references` tool for finding all usages
- File system watching for automatic cache invalidation
- LRU eviction policy for solution cache
- Performance optimizations for large solutions

### Improved
- Enhanced error messages with detailed context
- Better handling of multi-solution workspaces
- Optimized member discovery performance

### Fixed
- Memory leaks in solution cache
- Race conditions in concurrent solution access

---

## [0.3.0] - Phase 3 Completion (Planned)

### Added
- `find_implementations` tool for interface and base class discovery
- `get_type_hierarchy` tool for inheritance analysis
- `analyze_code_block` tool with semantic analysis
- `get_compilation_diagnostics` tool for error/warning reporting
- Nullability analysis support for nullable reference types
- Cancellation token support for all async operations
- Configurable timeout settings

### Improved
- Diagnostic message formatting
- Type hierarchy traversal performance
- Memory usage for large solutions

---

## [0.4.0] - Phase 4 Completion (Planned)

### Added
- `get_method_overloads` tool for overload discovery
- `suggest_using_directives` tool for namespace suggestions
- Support for implicit usings in .NET 6+
- Support for global using directives
- XML documentation comment extraction
- Smart filtering for using directive suggestions

### Improved
- Method signature formatting
- Documentation extraction accuracy
- Using directive relevance ranking

---

## [0.5.0] - Phase 5 Completion (Planned)

### Added
- Configuration system via environment variables and JSON config
- Structured logging with configurable levels
- Health check endpoint for monitoring
- Performance metrics collection
- Docker container support
- Comprehensive user documentation
- MCP client launch configurations

### Improved
- Startup performance
- Error recovery mechanisms
- Resource cleanup on shutdown

### Security
- Added input validation for all tool parameters
- Implemented path traversal protection

---

## [0.6.0] - Phase 6 Completion (Planned)

### Added
- Multi-targeting project support
- Project dependency graph analysis
- Source generator support
- Basic code actions and quick fixes
- F# language support (experimental)
- Razor file analysis
- Streaming support for large result sets

### Improved
- Cross-project reference performance
- Complex generic type handling
- Multi-language solution support

### Deprecated
- Legacy solution loading mechanism (replaced with MSBuild.Locator)

---

## Version History Summary

- **0.1.0**: Foundation - Basic type resolution and workspace management
- **0.2.0**: Navigation - Code navigation and member discovery
- **0.3.0**: Analysis - Advanced analysis and diagnostics
- **0.4.0**: Intelligence - Method and namespace intelligence
- **0.5.0**: Production - Configuration and deployment readiness
- **0.6.0**: Enterprise - Multi-project and advanced features