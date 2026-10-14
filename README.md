# ai-hero-rat

A local web-search MCP server for AI agents, shipped as a .NET global tool.

## Why "HeroRAT"

APOPO trains African giant pouched rats, the HeroRATs, to sniff out landmines and tuberculosis.
They are small, fast, and good at finding the one thing that matters in a lot of ground.
That is the job this project wants to do for an agent: go out, find the useful page, bring it back.

## Goal

Give Claude Code and other MCP-capable agents web search that runs on your own machine,
through a single dotnet tool that speaks the Model Context Protocol over stdio.

## Status

Initialization only. The tool builds, packs, and starts an MCP server that exposes **no tools yet**.
There is no web search in this release. See [the changelog](docs/changelog/README.md) for what has shipped.

## Install

Not published yet. Once a release is on nuget.org this will work:

```bash
dotnet tool install -g ai-hero-rat
```

## Build and test from source

You need the .NET SDK pinned in [`global.json`](global.json).

```bash
dotnet build AiHeroRat.slnx
dotnet test
dotnet pack src/AiHeroRat -c Release   # writes .nupkg/ai-hero-rat.<version>.nupkg
```

Commands the tool understands today:

- `ai-hero-rat serve` runs the MCP server on stdio. Stdout carries only JSON-RPC, logs go to stderr.
- `ai-hero-rat version` prints the bare version, for example `0.1.0`.

## Use it from Claude Code

After installing (or after a local `dotnet tool install` from the packed nupkg), register the server:

```bash
claude mcp add ai-hero-rat -- ai-hero-rat serve
```

Or add it to an MCP configuration file by hand:

```json
{
  "mcpServers": {
    "ai-hero-rat": {
      "command": "ai-hero-rat",
      "args": ["serve"]
    }
  }
}
```

The server connects, but lists no tools until search lands.

## Documentation

- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Architecture decisions](docs/adr/README.md)
- [Changelog](docs/changelog/README.md)

## License

MIT. See [LICENSE](LICENSE).
