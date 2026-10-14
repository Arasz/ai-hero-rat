using System.CommandLine;
using System.Reflection;
using AiHeroRat.Serve;
using AiHeroRat.Version;
using Spectre.Console;

var serve = new Command("serve", "Run the MCP server over stdio.");
serve.SetAction((_, cancellationToken) => new ServeCommand().RunAsync(cancellationToken));

var version = new Command("version", "Print the tool version.");
version.SetAction(_ =>
{
    var informationalVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    new VersionCommand(AnsiConsole.Console).Run(informationalVersion!);
});

var root = new RootCommand("MCP server for agents; local web search is in development.") { serve, version };
root.Options.Remove(root.Options.OfType<VersionOption>().Single());
return await root.Parse(args).InvokeAsync();
