# Security policy

## Reporting a vulnerability

**Do not open a public issue for a security problem.**

Report privately through GitHub's [private vulnerability reporting][pvr] (the
**Security -> Report a vulnerability** tab). If that is unavailable, email
**araszkiewiczrafal@gmail.com** with `ai-hero-rat security` in the subject.

Please include what an attacker can do and what they need to do it, the affected file or a
reproduction command, and the version (the repo-root [`VERSION`](VERSION) file, or the commit).

### What to expect

This is a **one-maintainer project**. There is no on-call rotation and no guaranteed response
time. Expect best effort, usually an acknowledgement within a week, and a fix shipped as a normal
version bump.

## Supported versions

Only the **latest release** is supported. Fixes ship forward; there are no backports.

## What this project is, security-wise

ai-hero-rat is a **local MCP server process**. It has no hosted service and no account.

| Surface | What it does today | Who controls the input |
|---------|--------------------|------------------------|
| `serve` over stdio | Reads MCP JSON-RPC from stdin, answers on stdout, logs to stderr. Exposes no tools yet. | The MCP client that launched the process |
| `version` | Prints the build version. | The local user |
| NuGet package | Ships the built tool via `dotnet pack`. Not published yet. | The pack and push commands |

The tool opens no network connection today.

## Notes for the web search to come

Once search exists, the tool will fetch content from the open web on an agent's behalf.
That changes the threat model, and these points will apply:

- **Fetched content is untrusted.** A page can contain text written to steer the agent
  reading it (prompt injection). Results should be treated as data, never as instructions.
- **Outbound requests are an SSRF risk.** Fetching a URL an agent chose can reach localhost,
  private networks, or cloud metadata endpoints unless the tool blocks those targets.
- **Secrets stay out of tracked files.** Any search-provider key must come from environment
  variables or user-scoped configuration, never from the repository.
- **Stdout is the protocol.** Anything written there other than JSON-RPC corrupts the session,
  so logs and diagnostics go to stderr.

None of this is implemented yet; it is the bar the first search release has to meet.

## What is deliberately not here yet

- No automated security scanning (CodeQL, Dependabot, secret scanning) is configured. CI builds,
  tests, and packs on every pull request and every push to `main`.
- No release automation or NuGet publishing exists yet.

## Out of scope

- Vulnerabilities in the [ModelContextProtocol C# SDK](https://www.nuget.org/packages/ModelContextProtocol)
  or the .NET runtime. Report those to their maintainers.
- Vulnerabilities in MCP clients. Report those upstream.
- Anything that needs write access to this repository or to the machine the tool runs on.

[pvr]: https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability
