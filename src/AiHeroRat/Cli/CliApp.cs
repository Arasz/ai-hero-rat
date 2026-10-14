using AiHeroRat.Serve;
using AiHeroRat.Version;
using Spectre.Console.Cli;

namespace AiHeroRat.Cli;

/// <summary>The command list of the tool, shared by Program and the tests.</summary>
internal static class CliApp
{
    public static void Configure(IConfigurator config)
    {
        config.SetApplicationName("ai-hero-rat");
        config.AddCommand<ServeCommand>("serve").WithDescription("Run the MCP server over stdio.");
        config.AddCommand<VersionCommand>("version").WithDescription("Print the tool version.");
    }
}
