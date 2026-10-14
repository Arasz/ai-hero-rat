using CommunityToolkit.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AiHeroRat.Version;

/// <summary>Writes the bare semver of the build to the console.</summary>
internal sealed class VersionCommand : Command
{
    private readonly IAnsiConsole _console;
    private readonly BuildVersion _version;

    public VersionCommand(IAnsiConsole console, BuildVersion version)
    {
        Guard.IsNotNull(console);
        Guard.IsNotNull(version);
        _console = console;
        _version = version;
    }

    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        _console.WriteLine(_version.Bare);
        return 0;
    }
}
