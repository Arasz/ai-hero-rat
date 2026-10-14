# ADR-0001: A .NET global tool serving MCP over stdio

Status: Accepted

## Context

The goal is local web search for Claude Code and other agents. Agents already launch MCP servers
as child processes and talk to them over stdin and stdout. The author maintains
[ai-raccoon](https://github.com/Arasz/ai-raccoon), a .NET MCP server shipped the same way, and
already has working conventions for build, tests, hooks, and releases.

## Decision

- Ship **ai-hero-rat** as a .NET 10 global tool (`dotnet tool install -g ai-hero-rat`) with the
  `McpServer` package type.
- Speak MCP over **stdio**, using the ModelContextProtocol C# SDK on the Generic Host.
  Stdout is reserved for JSON-RPC; all logging goes to stderr.
- Parse the command line with **System.CommandLine** and render console output through
  **Spectre.Console**, passed in as `IAnsiConsole` so tests can swap in a test console.
- Inherit ai-raccoon's conventions: VERSION-driven versioning, central package management,
  warnings as errors, xunit.v3 on the Microsoft Testing Platform, guard clauses from
  CommunityToolkit.Diagnostics, source-generated logging.

## Consequences

- One install command, no server to host, and the client owns the process lifetime.
- A stray `Console.WriteLine` in server code breaks the protocol, so an integration test checks that
  the first stdout line of `serve` is JSON-RPC.
- A remote or HTTP transport is out of scope for now. Adding one later needs its own ADR.
- The first search release must answer the open questions this ADR leaves alone: which search
  backend, and how fetched content is sanitized (see [SECURITY.md](../../SECURITY.md)).
