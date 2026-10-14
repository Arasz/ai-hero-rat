using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace AiHeroRat.Serve;

[Trait("Category", "Integration")]
public sealed class ServeStdoutTests
{
    private const string StartupLogLine = "MCP server listening on stdio";

    private const string InitializeRequest =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"stdout-test","version":"0.0.1"}}}""";

    [Fact]
    public async Task Serve_logs_to_stderr_and_answers_initialize_as_the_first_stdout_line()
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
        var stderr = new StringBuilder();
        var startupLogged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var process = new Process { StartInfo = startInfo };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            lock (stderr)
            {
                stderr.AppendLine(args.Data);
            }

            if (args.Data.Contains(StartupLogLine, StringComparison.Ordinal))
            {
                startupLogged.TrySetResult();
            }
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        process.Start();
        process.BeginErrorReadLine();
        try
        {
            await startupLogged.Task.WaitAsync(TimeSpan.FromSeconds(20), timeout.Token);

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
            await process.WaitForExitAsync(CancellationToken.None);
        }

        lock (stderr)
        {
            stderr.ToString().ShouldContain(StartupLogLine);
        }
    }
}
