using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli;

namespace AiHeroRat.Cli;

/// <summary>Spectre.Console.Cli registrar over the standard container; each run builds and disposes its own provider.</summary>
internal sealed class ContainerRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public ContainerRegistrar(IServiceCollection services)
    {
        Guard.IsNotNull(services);
        _services = services;
    }

    public void Register(Type service, Type implementation) => _services.TryAddTransient(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory) => _services.TryAddSingleton(service, _ => factory());

    public ITypeResolver Build() => new Resolver(_services.BuildServiceProvider());

    private sealed class Resolver(ServiceProvider provider) : ITypeResolver, IDisposable
    {
        public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

        public void Dispose() => provider.Dispose();
    }
}
