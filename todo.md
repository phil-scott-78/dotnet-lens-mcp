# Annotation-Based Type Testing Strategy

## Problem Statement
Current `GetTypeAtPositionTests.cs` uses hard-coded line/column positions which are extremely brittle. Any code changes break tests, making maintenance difficult and error-prone.

## Solution Overview
Replace position-based testing with annotation-driven approach where source files contain inline test metadata as end-of-line comments.

## Annotation Format

### Basic Structure
```csharp
var simpleString = "Hello, World!"; // @test:type target="simpleString" expect="string" kind="Local"
```

### Properties
- `target`: Text to locate on the line (identifier name)
- `expect`: Expected `FullTypeName` value  
- `kind`: Expected `Kind` value (Local, NamedType, Property, Parameter, etc.)
- `generic`: Boolean for generic types (optional)
- `args`: Comma-separated type arguments (optional)
- `occurrence`: Which occurrence of target text (default: 1)

### Complex Examples
```csharp
// Generic collections
List<string> stringList = new List<string>(); // @test:type target="stringList" expect="System.Collections.Generic.List<string>" kind="Local" generic="true" args="string"

// Multiple type parameters  
Dictionary<int, string> dict = new(); // @test:type target="dict" expect="System.Collections.Generic.Dictionary<int, string>" kind="Local" generic="true" args="int,string"

// Nested generics
List<Dictionary<string, int>> nested = new(); // @test:type target="nested" expect="System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, int>>" kind="Local" generic="true"

// Method parameters
public void Method(string text) // @test:type target="text" expect="string" kind="Parameter"

// Type parameters
public class Box<T> // @test:type target="T" expect="T" kind="TypeParameter"

// Properties
public string Name { get; set; } // @test:type target="Name" expect="string" kind="Property"

// Multiple occurrences
string first = "a", second = "b"; // @test:type target="second" expect="string" kind="Local" occurrence="2"

// Nullable types
int? nullable = 42; // @test:type target="nullable" expect="int?" kind="Local"

// Arrays
int[] array = new int[5]; // @test:type target="array" expect="int[]" kind="Local"

// Tuples
(int, string) tuple = (1, "one"); // @test:type target="tuple" expect="(int, string)" kind="Local"

// Tasks
Task<string> task = Task.FromResult("test"); // @test:type target="task" expect="System.Threading.Tasks.Task<string>" kind="Local" generic="true" args="string"
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. **AnnotationModels.cs** - Data structures for parsed annotations
2. **AnnotationParser.cs** - Parse `@test:type` comments from source lines
3. **TextPositionResolver.cs** - Convert target text to line/column positions
4. **AnnotatedTestDiscovery.cs** - Scan files and generate test cases

### Phase 2: Test Integration  
5. **AnnotatedTypePositionTests.cs** - xUnit test class using annotations
6. **TestBase classes** - Shared infrastructure for annotation-driven tests
7. **File scanning** - Discover all annotated playground files

### Phase 3: Migration & Validation
8. **Add annotations** to existing playground files
9. **Validate results** against current hard-coded tests  
10. **Replace old tests** with annotation-based approach
11. **Performance testing** with large codebases

## File Structure
```
DotnetLensMcp.Tests/
├── Infrastructure/
│   ├── Annotations/
│   │   ├── AnnotationModels.cs
│   │   ├── AnnotationParser.cs  
│   │   ├── TextPositionResolver.cs
│   │   └── AnnotatedTestDiscovery.cs
│   └── TestBase/
│       └── AnnotationTestBase.cs
├── Services/
│   ├── AnnotatedTypePositionTests.cs (NEW)
│   └── GetTypeAtPositionTests.cs (REPLACE/REMOVE)
```

## Detailed Implementation Steps

### 1. AnnotationModels.cs
```csharp
public record TypeTestAnnotation
{
    public string Target { get; init; } = "";
    public string Expect { get; init; } = "";
    public string Kind { get; init; } = "";
    public bool? Generic { get; init; }
    public string[]? Args { get; init; }
    public int Occurrence { get; init; } = 1;
    public string FilePath { get; init; } = "";
    public int LineNumber { get; init; }
    public string SourceLine { get; init; } = "";
}

public record PositionInfo(int Line, int Column);
```

### 2. AnnotationParser.cs
- Regex to extract `@test:type` annotations
- Parse key="value" attribute pairs
- Handle quoted strings with escaping
- Validate required properties

### 3. TextPositionResolver.cs  
- Find target text within source line
- Handle multiple occurrences
- Calculate precise character position
- Cache results for performance

### 4. AnnotatedTestDiscovery.cs
- Scan playground source files
- Extract all type test annotations
- Resolve text positions to line/column
- Generate test case metadata

### 5. AnnotatedTypePositionTests.cs
- Dynamic test generation using `[Theory]` and `[MemberData]`
- Load annotations from playground files
- Execute GetTypeAtPositionAsync calls
- Assert against annotation expectations

## Edge Cases to Handle

1. **Multiple occurrences** - Same identifier appears multiple times on line
2. **Whitespace variations** - Spaces/tabs around target text
3. **Comments in strings** - Ignore annotations inside string literals  
4. **Escaped quotes** - Handle `\"` in expected type names
5. **Line continuations** - Identifiers split across lines
6. **Generic constraints** - Complex generic type expressions
7. **Anonymous types** - Dynamic type name generation
8. **Nullable annotations** - C# 8.0+ nullable reference types
9. **Tuple names** - Named tuple elements
10. **Dynamic types** - Runtime type resolution

## Benefits

### Maintainability
- Tests automatically adapt to code changes
- No more broken tests from simple refactoring
- Easy to add new test cases

### Readability  
- Test expectations co-located with code
- Clear annotation format
- Self-documenting test scenarios

### Robustness
- Text-based targeting less brittle than positions
- Handles code formatting changes
- Supports complex type scenarios

### Productivity
- Faster test authoring
- Easier debugging when tests fail
- Better test coverage

## Validation Strategy

1. **Baseline comparison** - Run both old and new tests, compare results
2. **Known edge cases** - Test complex scenarios manually first  
3. **Performance testing** - Measure annotation parsing overhead
4. **Cross-platform** - Verify behavior on different OS/file encodings
5. **Large codebases** - Test with many annotated files

## Rollout Plan

### Week 1: Infrastructure
- Implement core annotation classes
- Create basic parsing and position resolution
- Unit tests for infrastructure components

### Week 2: Integration
- Build test discovery and runner
- Create first annotation-based tests
- Validate against existing test results

### Week 3: Migration  
- Add annotations to all playground files
- Replace hard-coded tests
- Performance optimization and edge case handling

### Week 4: Validation & Cleanup
- Comprehensive testing of new approach
- Remove old brittle tests
- Documentation and examples

## Success Criteria

1. ✅ All existing test scenarios covered by annotations
2. ✅ New annotation tests pass with same results as old tests
3. ✅ Easy to add new test cases by adding annotations
4. ✅ Tests remain stable during code refactoring
5. ✅ Performance acceptable for large codebases
6. ✅ Clear documentation and examples for future maintainers
