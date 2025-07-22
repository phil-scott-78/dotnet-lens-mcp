using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DotnetLensMcp.Tests.Fixtures;

public static class TestHelpers
{
    public static Task<Document> CreateTestDocumentAsync(string code, string fileName = "Test.cs")
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddDocument(documentId, fileName, SourceText.From(code, Encoding.UTF8));

        return Task.FromResult(solution.GetDocument(documentId)!);
    }

    public static (int line, int column) GetPositionFromIndex(string text, int index)
    {
        var lines = text.Split('\n');
        var currentIndex = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (currentIndex + lines[i].Length >= index)
            {
                return (i + 1, index - currentIndex + 1);
            }
            currentIndex += lines[i].Length + 1; // +1 for newline
        }
        
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public static string GetTestFilePath(string relativePath)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var searchDirectory = currentDirectory;
        
        while (!string.IsNullOrEmpty(searchDirectory))
        {
            var solutionFiles = Directory.GetFiles(searchDirectory, "dotnet-lens-mcp.sln");
            if (solutionFiles.Any())
            {
                return Path.Combine(Path.GetDirectoryName(solutionFiles.First())!, relativePath);
            }
            searchDirectory = Directory.GetParent(searchDirectory)?.FullName;
        }
        
        throw new InvalidOperationException("Could not find solution root");
    }
    
    public static string GetProjectDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var searchDirectory = currentDirectory;
        
        while (!string.IsNullOrEmpty(searchDirectory))
        {
            var solutionFiles = Directory.GetFiles(searchDirectory, "dotnet-lens-mcp.sln");
            if (solutionFiles.Any())
            {
                return Path.GetDirectoryName(solutionFiles.First())!;
            }
            searchDirectory = Directory.GetParent(searchDirectory)?.FullName;
        }
        
        throw new InvalidOperationException("Could not find solution root");
    }
    
    public static async Task<(int line, int column)> FindPositionInFileAsync(string filePath, string searchText, int occurrence = 1)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return FindPositionInText(content, searchText, occurrence);
    }
    
    public static (int line, int column) FindPositionInText(string text, string searchText, int occurrence = 1)
    {
        if (string.IsNullOrEmpty(searchText))
            throw new ArgumentException("Search text cannot be null or empty", nameof(searchText));
            
        int foundCount = 0;
        int searchIndex = 0;
        int targetIndex = -1;
        
        // Find the nth occurrence
        while (foundCount < occurrence)
        {
            var index = text.IndexOf(searchText, searchIndex, StringComparison.Ordinal);
            if (index == -1)
                throw new InvalidOperationException($"Could not find occurrence {occurrence} of '{searchText}' in text");
                
            foundCount++;
            if (foundCount == occurrence)
            {
                targetIndex = index;
                break;
            }
            searchIndex = index + 1;
        }
        
        // Calculate midpoint of the found text
        int position = targetIndex + (searchText.Length / 2);
        
        // Convert position to line and column
        int line = 1;
        int column = 1;
        
        for (int i = 0; i < position; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else if (text[i] != '\r') // Skip carriage returns
            {
                column++;
            }
        }
        
        return (line, column);
    }
}