using CommunityToolkit.Diagnostics;
using Spectre.Console;

namespace AiHeroRat.Version;

/// <summary>Writes the bare semver of a build to a console, dropping any <c>+sha</c> suffix.</summary>
internal sealed class VersionCommand
{
    private readonly IAnsiConsole _console;

    public VersionCommand(IAnsiConsole console)
    {
        Guard.IsNotNull(console);
        _console = console;
    }

    public void Run(string informationalVersion)
    {
        Guard.IsNotNullOrWhiteSpace(informationalVersion);
        var plusIndex = informationalVersion.IndexOf('+');
        var bare = plusIndex < 0 ? informationalVersion : informationalVersion[..plusIndex];
        _console.WriteLine(bare);
    }
}
