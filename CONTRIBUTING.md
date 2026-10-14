# Contributing

This is a one-maintainer project, so the workflow is short.

## Workflow

1. Branch from `main`. Never push to `main` directly.
2. Open a pull request, as a draft while the work is in progress.
3. Write the failing test first, then the code that makes it pass (TDD).
4. Keep commits small and conventional (`feat:`, `fix:`, `docs:`, `ci:`, `chore:`).

## Set up

```bash
brew install lefthook   # or any other way to get lefthook on your PATH
lefthook install
```

The hooks in [`lefthook.yml`](lefthook.yml) do two things. Before a commit, `dotnet format`
verifies the staged C# files. Before a push, the solution is built and the tests run.

## Build and test

```bash
dotnet build AiHeroRat.slnx
dotnet test
```

Tests marked `Category=Integration` launch the built tool as a child process.
They run with everything else; filter on the trait if you want to skip them.

## Conventions

- Nullable reference types on, warnings are errors, braces on every conditional and loop.
- Validate arguments with `CommunityToolkit.Diagnostics` guards, not hand-written throws.
- Log through nested `[LoggerMessage]` methods, never direct `ILogger` calls.
- Keep doc comments to one to three lines. Name things plainly.
- In the MCP server, stdout belongs to the protocol. Send every log and any console output to stderr.
- NuGet versions live in `Directory.Packages.props` (source) and `tests/Directory.Packages.props` (tests).

## Releases

Every release bumps the [`VERSION`](VERSION) file (semver: patch for fixes, minor for features,
major for breaking changes) and adds `docs/changelog/{version}-{slug}.md`.
The format is described in [the changelog README](docs/changelog/README.md).
Add an [ADR](docs/adr/README.md) for any architecture-level decision.
