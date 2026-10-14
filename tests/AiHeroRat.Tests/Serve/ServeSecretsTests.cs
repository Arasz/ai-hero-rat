using System.Reflection;
using Microsoft.Extensions.Configuration.UserSecrets;
using Shouldly;
using Xunit;

namespace AiHeroRat.Serve;

/// <summary>Redirects the user-secrets root through APPDATA, so it must not run alongside other tests.</summary>
[CollectionDefinition(DisableParallelization = true)]
public sealed class EnvironmentVariableCollection;

[Collection(nameof(EnvironmentVariableCollection))]
public sealed class ServeSecretsTests
{
    [Fact]
    public void Serve_configuration_reads_search_api_keys_from_the_tool_user_secrets()
    {
        var secretsId = typeof(ServeCommand).Assembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
        secretsId.ShouldNotBeNull();
        var root = Directory.CreateTempSubdirectory("ai-hero-rat-secrets-");
        var store = Directory.CreateDirectory(Path.Combine(root.FullName, "Microsoft", "UserSecrets", secretsId));
        File.WriteAllText(Path.Combine(store.FullName, "secrets.json"), """{"Search:ApiKey:Brave":"test-brave-key"}""");
        var previousAppData = Environment.GetEnvironmentVariable("APPDATA");
        Environment.SetEnvironmentVariable("APPDATA", root.FullName);
        try
        {
            var configuration = ServeCommand.CreateBuilder().Configuration;

            configuration["Search:ApiKey:Brave"].ShouldBe("test-brave-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", previousAppData);
            root.Delete(recursive: true);
        }
    }
}
