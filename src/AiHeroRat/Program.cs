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
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    new VersionCommand(AnsiConsole.Console).Run(informationalVersion);
});

var root = new RootCommand("ai-hero-rat: local web search for AI agents over MCP.") { serve, version };
return await root.Parse(args).InvokeAsync();
