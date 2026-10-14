using AiHeroRat.Cli;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace AiHeroRat.Version;

public sealed class VersionCommandTests
{
    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("2.0.0-rc.1+deadbeef", "2.0.0-rc.1")]
    public void Version_prints_the_bare_version_without_the_build_suffix(string informationalVersion, string expected)
    {
        var console = new TestConsole();
        var tester = NewTester(console, new BuildVersion(informationalVersion));

        var result = tester.Run("version");

        result.ExitCode.ShouldBe(0);
        console.Output.Trim().ShouldBe(expected);
    }

    private static CommandAppTester NewTester(TestConsole console, BuildVersion version)
    {
        var services = new ServiceCollection();
        services.AddSingleton<Spectre.Console.IAnsiConsole>(console);
        services.AddSingleton(version);
        var tester = new CommandAppTester(new ContainerRegistrar(services));
        tester.Configure(CliApp.Configure);
        return tester;
    }
}
