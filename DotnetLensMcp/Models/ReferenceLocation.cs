namespace DotnetLensMcp.Models;

public class ReferenceLocation
{
    public string FilePath { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string LineText { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}