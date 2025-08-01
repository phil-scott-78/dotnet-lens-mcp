using DotnetLensMcp.Services;
using DotnetLensMcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add Roslyn services
builder.Services.AddSingleton<SolutionCache>();
builder.Services.AddSingleton<WorkspaceResolver>();
builder.Services.AddSingleton<RoslynService>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<RoslynTools>();

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.SetMinimumLevel(LogLevel.Error);
    loggingBuilder.AddConsole(o =>
    {
        o.LogToStandardErrorThreshold = LogLevel.Trace;
    });
});

await builder.Build().RunAsync();
