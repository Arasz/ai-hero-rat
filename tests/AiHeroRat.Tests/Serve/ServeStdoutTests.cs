using System.Diagnostics;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace AiHeroRat.Serve;

[Trait("Category", "Integration")]
public sealed class ServeStdoutTests
{
    private const string InitializeRequest =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"stdout-test","version":"0.0.1"}}}""";

    [Fact]
    public async Task Serve_writes_only_json_rpc_to_stdout()
    {
        var toolDll = typeof(ServeCommand).Assembly.Location;
        var startInfo = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { "exec", toolDll, "serve" },
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.StandardInput.WriteLineAsync(InitializeRequest.AsMemory(), timeout.Token);
            await process.StandardInput.FlushAsync(timeout.Token);

            var firstLine = await process.StandardOutput.ReadLineAsync(timeout.Token);

            firstLine.ShouldNotBeNull();
            using var message = JsonDocument.Parse(firstLine);
            message.RootElement.GetProperty("jsonrpc").GetString().ShouldBe("2.0");
            message.RootElement.GetProperty("id").GetInt32().ShouldBe(1);
            message.RootElement.TryGetProperty("result", out _).ShouldBeTrue();
        }
        finally
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
