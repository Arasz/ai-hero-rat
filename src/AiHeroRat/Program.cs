using System.Reflection;
using AiHeroRat.Cli;
using AiHeroRat.Version;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

var informationalVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddSingleton(new BuildVersion(informationalVersion!));

var app = new CommandApp(new ContainerRegistrar(services));
app.Configure(CliApp.Configure);
return await app.RunAsync(args);
