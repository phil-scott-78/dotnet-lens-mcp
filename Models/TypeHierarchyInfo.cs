namespace RoslynMcp.Models;

public class TypeHierarchyInfo
{
    public List<TypeInfo> BaseTypes { get; set; } = new();
    public List<TypeInfo> DerivedTypes { get; set; } = new();
    public List<TypeInfo> Interfaces { get; set; } = new();
}

public class TypeInfo
{
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string? Assembly { get; set; }
}