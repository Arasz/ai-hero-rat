using Shouldly;
using Spectre.Console.Testing;
using Xunit;

namespace AiHeroRat.Version;

public sealed class VersionCommandTests
{
    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("2.0.0-rc.1+deadbeef", "2.0.0-rc.1")]
    public void Run_writes_the_bare_version_without_the_build_suffix(string informationalVersion, string expected)
    {
        var console = new TestConsole();

        new VersionCommand(console).Run(informationalVersion);

        console.Output.Trim().ShouldBe(expected);
    }
}
