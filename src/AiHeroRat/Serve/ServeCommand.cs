using Microsoft.Extensions.Configuration;
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
        using var host = CreateBuilder().Build();
        Log.Serving(host.Services.GetRequiredService<ILogger<ServeCommand>>());
        await host.RunAsync(cancellationToken);
        return 0;
    }

    /// <summary>The serve host: user-secrets configuration (search API keys), stdio MCP transport and stderr-only logging.</summary>
    internal static HostApplicationBuilder CreateBuilder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddUserSecrets(typeof(ServeCommand).Assembly, optional: true);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddMcpServer().WithStdioServerTransport();
        return builder;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "MCP server listening on stdio")]
        public static partial void Serving(ILogger logger);
    }
}
