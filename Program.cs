using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoslynMcp.Services;
using RoslynMcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Add Roslyn services
builder.Services.AddSingleton<SolutionCache>();
builder.Services.AddSingleton<WorkspaceResolver>();
builder.Services.AddSingleton<RoslynService>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RoslynTools>();

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddConsole(o =>
    {
        o.LogToStandardErrorThreshold = LogLevel.Trace;
    });
});

await builder.Build().RunAsync();
