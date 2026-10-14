using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace AiHeroRat.Serve;

/// <summary>Runs the MCP server over stdio. Stdout carries only JSON-RPC; all logging goes to stderr.</summary>
internal sealed partial class ServeCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddMcpServer().WithStdioServerTransport();

        using var host = builder.Build();
        Log.Serving(host.Services.GetRequiredService<ILogger<ServeCommand>>());
        await host.RunAsync(cancellationToken);
        return 0;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "MCP server listening on stdio")]
        public static partial void Serving(ILogger logger);
    }
}
