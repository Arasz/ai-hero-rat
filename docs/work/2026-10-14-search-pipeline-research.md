# Research: inputs for the search and extraction pipeline with an Obscura fallback

**Date:** 2026-10-14
**Question:** What are the exact contracts, measured behaviours and limits that the ai-hero-rat search pipeline (TinyFish → Brave → Exa search, then Obscura `--stealth` engine scraping; TinyFish fetch, then Obscura HTML + SmartReader extraction; proxied Obscura MCP tools) will be built against?

Owner decisions taken before this research (2026-10-14): search order TinyFish → Brave → Exa, then Obscura. The fallback may scrape any engine measured to return results, and robots.txt is not a filter; the owner accepts that risk. Obscura is auto-downloaded from a pinned release with a pinned sha256. The task runs the high-effort loop.

Method: three read-only lanes, plus one engine sweep that the owner ran by hand. Lane A read API docs and made no keyed calls. Lane B measured Obscura's MCP server and release assets. Lane C read and ran the MCP C# SDK 2.2.0. Claude Code's auto-mode classifier denied the engine sweep to the lane, so the owner ran `scratchpad/research/engine-sweep.sh` once on this Mac (macOS 27.0.1, arm64, residential IP) at about 19:00 UTC, and the saved HTML was analysed offline. Lane reports and raw outputs are in the session scratchpad and are not committed. Findings copied from a lane are graded by what the lane did.

```chart:matrix
title: organic results parsed from one Obscura --stealth fetch per engine (query "dotnet mcp server")
engine, results
duckduckgo-html, 10
bing, 10
yandex, 13
duckduckgo-lite, 5
yahoo, 0
qwant, 0
brave, 0
google, 0
mojeek, 0
startpage, 0
ecosia, 0
```

## Findings

### F1 — Through Obscura `--stealth`, Bing, DuckDuckGo (html and lite), Yandex, Yahoo and Qwant served result pages; Brave, Google, Mojeek, Startpage and Ecosia did not [MEASURED]

Each engine was fetched once with `obscura fetch <url> --stealth --dump html --timeout 30 --quiet`, with 2 s between engines. Every run exited 0 (wall time 0.65–5.38 s).
- **Served results:** Bing (title "dotnet mcp server - Search", 10 `b_algo` blocks), DuckDuckGo html and lite (10 results each), Yahoo ("… - Yahoo Search Results", 32 `algo`/`compTitle` markers), Qwant ("… – Qwant Search") and Yandex ("found 39 thousand results").
- **Blocked:** Brave (CAPTCHA), Google ("unusual traffic"), Mojeek (page titled "Captcha"), Ecosia (Cloudflare "Just a moment...") and Startpage (an empty page, 22 KB, no title).

DuckDuckGo served results here, though plain curl got its bot challenge earlier the same day (`2026-10-14-search-backend-options.md` F5). Brave went the other way: it served results to curl and a CAPTCHA to Obscura.

**Evidence:** the owner ran `engine-sweep.sh` (rc, wall time and bytes table pasted into the session). Then, offline, `grep -o` marker counts and `<title>` on `scratchpad/research/lane-b/engines/*.html`.

### F2 — Title, URL and snippet parse cleanly from DuckDuckGo html, Bing and Yandex; Yahoo and Qwant need real parsers [MEASURED]

A regex pass over the saved HTML got:
- **DuckDuckGo html:** 10/10 results with snippets, all relevant (Microsoft Learn MCP quickstart, the .NET blog, `modelcontextprotocol/csharp-sdk`).
- **DuckDuckGo lite:** 5 results.
- **Yandex:** 13 results with direct URLs and snippets.
- **Bing:** 10/10 results with snippets, but relevance was weak: the top three were generic .NET download and documentation pages.

The rough patterns found 0 results on Yahoo and Qwant, though their pages contain results (F1). Real selectors for those two are not written yet.

**Evidence:** `python3 -I scratchpad/research/lane-b-scripts/extract_engines.py scratchpad/research/lane-b/engines`, one run.

### F3 — Engine result links are wrapped differently per engine [MEASURED]

- **DuckDuckGo:** links go through `/l/?uddg=<percent-encoded target>`; unwrap with `uddg`.
- **Bing:** links go through `/ck/a?…&u=a1<base64url>`; drop the `a1` prefix and base64url-decode (checked on two links from the saved page).
- **Yandex:** organic results carry direct URLs.

**Evidence:** the `unwrap()` output of `extract_engines.py`, plus a one-off decode of two `bing.com/ck/a` hrefs from `bing.html`.

### F4 — TinyFish search is `GET https://api.search.tinyfish.ai` with an `X-API-Key` header [READ]

- **Parameters:** `query`, plus optional `location`, `language`, `include_domains`, `exclude_domains`, `domain_type` and `page` (0–10).
- **Response:** `{query, results[{position, site_name, title, snippet, url, date?}], total_results, page}`.
- **Limits:** 30 requests a minute per key; 12,000 a day free.
- **Errors:** 401 for a bad key, 402 with `INSUFFICIENT_CREDITS` or `FREE_ALLOWANCE_EXHAUSTED`, and 429 for the rate limit.

**Evidence:** lane A read https://docs.tinyfish.ai/search-api/reference, https://docs.tinyfish.ai/authentication and https://www.tinyfish.ai/blog/search-and-fetch-are-now-free-for-every-agent-everywhere.

### F5 — TinyFish fetch is `POST https://api.fetch.tinyfish.ai` and reports per-URL failures inside HTTP 200 [READ]

- **Request body:** `{urls[≤10], format: markdown|html|json, links, ttl, per_url_timeout_ms, include_selectors, exclude_selectors}`.
- **Response:** `{results[{url, final_url, title, description, language, author, published_date, text, latency_ms, format}], errors[{url, error, status?}]}`.
- **Failure codes:** include `bot_blocked`, `timeout`, `empty_content` and `page_not_found`. A client must check `errors[]`, not just the HTTP status.
- **Limits:** 150 URLs a minute and 1,000 a day free. Lane A recommends a client timeout of 150 s or more.

**Evidence:** lane A read https://docs.tinyfish.ai/fetch-api/reference.

### F6 — Brave is `GET https://api.search.brave.com/res/v1/web/search` with `X-Subscription-Token` [READ]

- **Parameters:** `q` (≤600 characters), `count` (1–20), `offset` (0–9), `country`, `search_lang`, `freshness`, `extra_snippets=true` and `text_decorations=false`.
- **Results:** `web.results[{title, url, description, page_age, age, extra_snippets[]}]`.
- **Errors:** `{type:"ErrorResponse", error:{id, status, code}}` with `code` in `INTERNAL|QUOTA_LIMITED|RATE_LIMITED`.
- **Rate-limit headers:** `X-RateLimit-Limit/Policy/Remaining/Reset`.

**Evidence:** lane A read https://api-dashboard.search.brave.com/api-reference/web/search/get and https://api-dashboard.search.brave.com/documentation/guides/rate-limiting.

### F7 — Exa is `POST https://api.exa.ai/search` with `x-api-key` [READ]

- **Request body:** `query`, `numResults` (1–100), `type`, `contents{text|highlights|summary, maxAgeHours}`.
- **Response:** `results[{title, url, publishedDate, author, text, highlights[], summary}]` plus `costDollars`.
- **Errors:** `{requestId, error, tag}`: 401 `INVALID_API_KEY`, 402 `NO_MORE_CREDITS`, 429 for the rate limit (10 QPS on `/search`).

**Evidence:** lane A read https://exa.ai/docs/reference/search, https://exa.ai/docs/reference/error-codes and https://exa.ai/docs/reference/rate-limits.

### F8 — SmartReader 0.11.1 parses HTML we already hold with `new Reader(uri, html).GetArticle()` and has no Markdown output [MEASURED]

- **Package:** Apache-2.0, netstandard2.0/2.1, depends on AngleSharp 1.5.2. Lane A compiled and ran it on net10.0.
- **Article fields:** `Title`, `Byline`, `Author`, `Excerpt`, `TextContent`, `Content` (HTML), `Length`, `IsReadable` and `Completed`.
- **Gotchas:**
  - `Reader.ParseArticle(url, html)` binds to the URL-plus-user-agent overload and fetches over the network. Avoid it.
  - Pages under 500 characters come back `IsReadable=false`.
  - `GetImagesAsync` and `ConvertImagesToDataUriAsync` make network calls.

The Markdown tier therefore needs a separate HTML-to-Markdown converter; ReverseMarkdown 6.2.1 is the candidate from `2026-10-14-search-backend-options.md` F9.

**Evidence:** lane A's throwaway net10.0 project in the session scratchpad, and https://api.nuget.org/v3-flatcontainer/smartreader/index.json.

### F9 — `obscura mcp` is a stdio MCP server with 37 tools that return text blocks [MEASURED]

- **Handshake:** initialize returns protocol `2024-11-05`, serverInfo `obscura-mcp 0.2.4` and capabilities `{tools:{}}`.
- **Tools:** tools/list gives the same 37 tools with and without `--stealth`. They include `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_fill`, `browser_type`, `browser_markdown`, `browser_links`, `browser_extract` (CSS-selector schema returning JSON), `browser_screenshot`, `browser_pdf`, and tab, cookie and storage tools.
- **Not web search:** `browser_search` finds text within the current page.
- **Result shape:** `browser_navigate` and `browser_snapshot` on example.com, in plain mode, returned only `text` content blocks.

**Evidence:** lane B's JSON-RPC driver `scratchpad/research/lane-b-scripts/mcp.py`; the outputs are `lane-b/mcp-stealth.json` and `lane-b/mcp-plain-nav.json`.

### F10 — Release assets follow one name pattern and each carries a sha256 digest; plain `fetch` runs without `obscura-worker` [MEASURED]

- **Name pattern:** `obscura-{x86_64|aarch64}-{linux|macos|windows}[-no-render][-stealth].{tar.gz|zip}`. Windows exists only as x86_64 `.zip`; there is no Windows arm64 build.
- **Digests:** the GitHub release API gives each asset a `digest` (sha256). A curl-downloaded macOS tarball matched its digest and runs with no quarantine flag (`xattr` shows only `com.apple.provenance`).
- **Worker:** plain `obscura fetch https://example.com` succeeded with the worker binary absent.

**Evidence:** lane B ran `gh api repos/h4ckf0r0day/obscura/releases/tags/v0.2.4` (saved as `lane-b/assets.json`), `xattr -l`, and a fetch from a directory holding only `obscura`.

### F11 — The MCP C# SDK 2.2.0 can act as Obscura's client and proxy its tools next to our own [MEASURED]

- **Client:** `McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions{ Command, Arguments, EnvironmentVariables, … }))`, then `ListToolsAsync()` and `CallToolAsync(new CallToolRequestParams{ Name, Arguments })`. Image and text blocks come back intact.
- **Child crash:** the in-flight call throws `ClientTransportClosedException`, later calls throw `IOException`, and the client never restarts the child.
- **Proxying next to our tools:** `.WithTools<T>().WithListToolsHandler(…).WithCallToolHandler(…)` merges both lists. tools/call tries attribute tools first and then the handler, for any other name, including unknown ones.

**Evidence:** lane C's throwaway project `scratchpad/mcpexp/` against ModelContextProtocol 2.2.0, with citations into the package XML docs in `scratchpad/research/lane-c-mcp-sdk.md` §1–2.

### F12 — Returning `CallToolResult { IsError = true }` with text is the only error form whose message reaches the agent verbatim [MEASURED]

- **`CallToolResult { IsError = true, Content = [TextContentBlock] }`:** the client sees the exact text.
- **`McpException("m")`:** becomes "An error occurred invoking 'x': m".
- **Any other exception:** becomes only "An error occurred invoking 'x'.", so the message is hidden.
- **`[McpMeta("anthropic/maxResultSizeChars", 500000)]`:** appears as `_meta` in tools/list.
- **Testing:** an in-memory server and client pair over two `System.IO.Pipelines.Pipe` objects (`WithStreamServerTransport` / `StreamClientTransport`) works for xunit tests.

**Evidence:** `scratchpad/research/lane-c-mcp-sdk.md` §3–5, measured in `scratchpad/mcpexp/`.

### F13 — The pipeline needs its own block detection, unwrap rules and robots-independent engine list [INFERRED]

From F1–F3 and `2026-10-14-obscura-fetch-test.md` F6, F7 and F9:
- **Block detection:** Obscura exits 0 on challenge pages, so every engine parser needs a "no results, blocked" signal.
- **Engine order:** from the parse quality in F2 and the relevance in F1–F2, DuckDuckGo html → Yandex → Bing, with lite as a backup.
- **Cleanup:** result URLs need per-engine unwrapping, and Obscura page dumps need SmartReader plus a Markdown converter.

## Still open

- **Repeatability:** each engine was hit once from one residential IP. Whether DuckDuckGo, Yandex and Bing keep serving Obscura under repeated queries, or from other IPs, is unknown. The pipeline should treat any engine as able to fail at any time.
- **Yahoo and Qwant parsers:** they served results (F1), but no working selector exists yet (F2).
- **Stealth MCP behaviour:** `obscura mcp --stealth` navigation, startup time and stderr noise were not exercised (lane B and lane C UNVERIFIED lists).
- **Stealth without the worker:** whether `fetch --stealth` needs `obscura-worker` is untested (F10).
- **Brave error statuses:** the HTTP status for an invalid key, and how `QUOTA_LIMITED` maps to a status, are not documented (lane A).
- **SmartReader network access:** whether the text-input path does any network access is untraced (lane A).
- **Unknown tool names:** which exception or code to throw from `CallToolHandler` for a name we don't own is unverified (lane C).
- **Live keyed calls:** no call was made with a real key, so response shapes come only from docs.
