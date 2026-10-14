# Research: search backend and tiered JSON results for ai-hero-rat

**Date:** 2026-10-14
**Question:** Which way should ai-hero-rat turn an agent's query into results on the local machine (a headless browser via Playwright, a plain HTTP client, or a search-engine API), and how should it return structured JSON at three tiers: URLs only, content summary, full page?

Method: three read-only research lanes (search APIs, local scraping and extraction, MCP protocol and prior art), run on 2026-10-14 from one macOS machine on a residential connection. The author re-checked the load-bearing claims (F1, F2, F4, F6, F9, F12, F13) against their sources. Where a lane ran a probe and the author re-read its saved output rather than re-running it, the evidence line says so.

```chart:bars
title: list price, USD per 1k searches, entry paid tier (READ, vendor pricing pages)
Exa instant: 4
Brave Search: 5
Google CSE (existing customers only): 5
Exa auto: 7
SerpAPI starter: 25
```

## Findings

### F1 — Bing's Web Search API no longer exists [READ]

Microsoft retired the Bing Search APIs. The replacement, "Grounding with Bing Search", runs inside Azure AI Agents, so it can't replace a plain search API in a local tool.

**Evidence:** https://learn.microsoft.com/en-us/lifecycle/announcements/bing-search-api-retirement says "Bing Search APIs will be retired on August 11, 2025." The author fetched it on 2026-10-14.

### F2 — Google's Custom Search JSON API takes no new customers and ends on 2027-01-01 [READ]

A new project can't get a key. Existing customers keep 100 free queries a day and pay $5 per 1k after that, until the shutdown.

**Evidence:** https://developers.google.com/custom-search/v1/overview says "The Custom Search JSON API is closed to new customers." and "Existing Custom Search JSON API customers have until January 1, 2027 to transition to an alternative solution." The author fetched it on 2026-10-14.

### F3 — Several keyed search APIs remain, priced at $4–25 per 1k searches [READ]

- Brave Search API: $5 per 1k with $5 of free credit a month (about 1k searches). Needs a card. Storing results needs a plan that grants storage rights.
- Tavily: 1,000 free credits a month, no card, then $0.008 per credit. The pricing page does not say how many credits one search costs.
- Exa: $10 of free credit a month, then $4 per 1k (instant) to $15 per 1k (deep). Page contents cost $1 per 1k, and summaries another $1 per 1k.
- SerpAPI: 250 free searches a month; the starter plan is $25 for 1k. It scrapes Google, so its legal footing is contested.
- Mojeek: £1–3 per 1k. Non-business plans may cache results for 1 hour only.
- Jina `s.jina.ai`: needs a free key, returns the top 5 results with their content, priced in tokens.

**Evidence:** a research lane read https://brave.com/search/api, https://tavily.com/pricing, https://exa.ai/pricing, https://serpapi.com/pricing and https://jina.ai/reader. The Mojeek figures came from a search-engine summary of mojeek.com/services/api.html because the direct fetch returned 404. The author did not re-open these pages.

### F4 — DuckDuckGo's public API returns no web results [MEASURED]

The Instant Answer API answers only encyclopedic queries. A normal web query comes back empty, so DuckDuckGo results are only reachable by scraping its HTML pages.

**Evidence:** run by the author on 2026-10-14 on this Mac: `curl -sS 'https://api.duckduckgo.com/?q=best+dotnet+mcp+server+tutorial&format=json&no_html=1'` returned `Results: 0, RelatedTopics: 0, Abstract: '', Answer: ''`. One run.

### F5 — Plain HTTP requests to result pages got real results only from Bing and Brave [MEASURED]

| Engine | HTTP | Results | What came back |
|---|---|---|---|
| html.duckduckgo.com | 202 | 0 | "anomaly" bot challenge |
| lite.duckduckgo.com | 202 | 0 | same challenge |
| google.com/search | 200 | 0 | JavaScript wall (`/httpservice/retry/enablejs`) |
| bing.com/search | 200 | 10 (`b_algo`) | real results |
| search.brave.com | 200 | 20 (`data-type="web"`) | real results |
| mojeek.com/search | 200 | 0 | "Captcha" page that needs JavaScript |

This is one request per engine from one home IP. It says nothing about repeated queries or datacenter IPs.

**Evidence:** a research lane ran `curl -s -L -m 20 -A "<Chrome 130 macOS UA>" -H 'Accept-Language: en-US' "<engine>?q=dotnet+mcp+server"` once per engine on 2026-10-14, on this Mac. The author re-counted the result markers in the saved responses (scratchpad `lane-b/p1–p6.html`) with `grep -ao` and got the counts above.

### F6 — Google, DuckDuckGo, Brave and Mojeek forbid crawlers on their search pages in robots.txt; Bing does not [MEASURED]

Scraping any of the four breaks their stated crawler policy. Bing's `/search` has no rule under `User-agent: *`, though many of its sub-search paths are disallowed. Nobody read any of the engines' terms of service, so the legal position of Bing scraping is still open.

**Evidence:** the author ran `curl https://<host>/robots.txt` on 2026-10-14.
- Google: `Disallow: /search` (only `/search/about` and `/search/howsearchworks` are allowed).
- Brave: `Disallow: /search`, with only `/search?conversation=` allowed.
- DuckDuckGo: `Disallow: /lite`, `Disallow: /html`.
- Bing: no `/search` line. It does have `Disallow: /academic/search`, `/copilotsearch?q=` and others.
- Mojeek (`Disallow: /search`) was fetched by the research lane only.

### F7 — Headless Chromium did no better than curl on the engines tried [MEASURED]

Chromium got real results from Bing and was blocked by DuckDuckGo, the same as curl. So Playwright adds no reach for the search step itself, at least from this IP. It costs a separate browser download: the headless shell takes 195 MB on disk, and full Chromium 359 MB.

**Evidence:** a research lane timed the Playwright headless-shell binary directly (`chrome-headless-shell --no-sandbox --disable-gpu --dump-dom <url>`, build 1243 = Chrome for Testing 153.0.8010.12) on 2026-10-14, one run each:
- example.com: 0.81 s
- Bing: 0.62 s, 10 results
- DuckDuckGo html: 0.84 s, 274 bytes, 0 results

The author re-measured the sizes with `du -sh ~/Library/Caches/ms-playwright/chromium_headless_shell-1243` (195M) and `chromium-1243` (359M). Nobody timed the Playwright .NET API itself.

### F8 — SearXNG gives a key-free JSON search endpoint, but only once JSON is enabled and the container is running [READ]

SearXNG is a self-hosted metasearch engine. Its JSON output is off by default; adding `json` to `search.formats` in `settings.yml` enables `GET /search?q=…&format=json`. It runs as a Docker image (about 265 MB built). It queries Google, Bing, DuckDuckGo and others itself, so the blocking in F5 still hits individual engines inside it.

**Evidence:** https://docs.searxng.org/admin/settings/settings_search.html, read by a research lane. The engine-blocking point is inferred from F5, not tested through SearXNG.

### F9 — Current .NET libraries cover article extraction and HTML-to-Markdown [MEASURED]

- SmartReader 0.11.1 (Apache-2.0): a port of Mozilla's Readability. It extracts the main article as title, byline, excerpt, plain text and content HTML.
- ReverseMarkdown 6.2.1 (MIT): converts HTML to Markdown.
- AngleSharp and HtmlAgilityPack: HTML parsers for pulling snippets out of result pages.
- Microsoft.Playwright 1.63.0 (MIT) is the current browser automation package.

Nobody has run SmartReader or ReverseMarkdown on real pages yet, so output quality is untested.

**Evidence:** the author queried `https://api.nuget.org/v3-flatcontainer/<id>/index.json` on 2026-10-14; the latest stable versions are smartreader 0.11.1, reversemarkdown 6.2.1 and microsoft.playwright 1.63.0. Release dates and licences come from the NuGet registration API, read by a research lane.

### F10 — Claude Code's own WebSearch returns titles and URLs only, and WebFetch summarizes pages through a second model [READ]

So ai-hero-rat beats the built-in tools only if it returns snippets and richer metadata at the URL tier, and raw, unsummarized, paginated text at the full-page tier.

**Evidence:** https://code.claude.com/docs/en/tools-reference ("WebSearch tool behavior", "WebFetch tool behavior"), read by a research lane. Per that page, WebFetch reads up to 100,000 characters, caches for 15 minutes and passes the page through a model with the caller's prompt.

### F11 — The MCP spec has typed tool output, and the C# SDK 2.2.0 exposes it as an opt-in [READ]

Spec revision 2025-06-18 defines `outputSchema` and `structuredContent`. A tool that declares a schema MUST return conforming results, and SHOULD also return the same JSON as a text block. In the SDK, `[McpServerTool(UseStructuredContent = true)]` turns it on and infers the schema from the return type; it is off by default.

**Evidence:** https://modelcontextprotocol.io/specification/2025-06-18/server/tools says "For backwards compatibility, a tool that returns structured content SHOULD also return the serialized JSON in a TextContent block." The author checked the SDK side in `~/.nuget/packages/modelcontextprotocol.core/2.2.0/lib/net10.0/ModelContextProtocol.Core.xml:15334` (`McpServerToolAttribute.UseStructuredContent`).

### F12 — Claude Code limits how much text a tool result can carry [READ]

- It warns above 10,000 tokens and cuts off at 25,000 by default (`MAX_MCP_OUTPUT_TOKENS`).
- A text result over 50,000 characters is saved to a file and replaced by a file reference.
- A tool can raise its own limit to at most 500,000 characters by setting `_meta["anthropic/maxResultSizeChars"]` in `tools/list`.

These limits set the size budgets for the three tiers.

**Evidence:** https://code.claude.com/docs/en/mcp, fetched by the author on 2026-10-14. It says "Claude Code displays a warning when MCP tool output exceeds 10,000 tokens and limits output to 25,000 tokens by default" and "Claude Code raises that tool's threshold to the annotated value, up to a hard ceiling of 500,000 characters."

### F13 — The official `fetch` MCP server shows how to page through large content [READ]

It takes `url`, `max_length` (default 5000), `start_index` and `raw`, and converts pages to Markdown with readabilipy. The full-page tier should follow the same paging pattern.

**Evidence:** https://github.com/modelcontextprotocol/servers/blob/main/src/fetch/README.md, read by a research lane.

### F14 — Claude Code reportedly mishandles results that carry both `content` and `structuredContent` [INFERRED]

User-filed issues (#59480, #64316, #45575, #55677) report that one of the two is dropped, and they disagree on which one. One issue (#53453) reports that `resource_link` blocks are ignored. This is inferred from a search-engine summary of those issues; nobody opened them or tested a current Claude Code release. If it holds, JSON in a text block is the safe default, and `structuredContent` should stay opt-in until a test against a real client passes.

### F15 — Claude Code does not support MCP sampling, so the summary tier can't ask the client's model to summarize [INFERRED]

Inferred from three things: a still-open feature request (anthropics/claude-code#1785, June 2025), a third-party client matrix that marks sampling unsupported, and the Claude Code MCP page listing elicitation but not sampling among client capabilities. Nobody confirmed this against an October 2026 release. The design consequence is that summaries must be produced without an LLM: an excerpt, the meta description, or the lead paragraphs.

### F16 — A provider interface lets the key-free and keyed backends share one tool [INFERRED]

From F1–F8: no single backend is free, key-free, permitted by robots.txt and reliable at the same time.
- Brave, Tavily and Exa are reliable and allowed, but need a key.
- SearXNG needs no key, but needs Docker and inherits engine blocking.
- Bing HTML scraping needs neither, but its terms are unread and it may get blocked under load.

An `ISearchProvider` that returns ranked `{title, url, snippet}` keeps that choice in configuration. A separate fetch-and-extract pipeline (HTTP → SmartReader → ReverseMarkdown) then serves tiers 2 and 3 whichever provider found the URLs.

### F17 — Playwright earns a place only as an optional fetcher for pages built by JavaScript [UNVERIFIED]

F7 shows it adds nothing for the search step on the engines tried. Whether a useful share of result pages need JavaScript to show their content was not measured. A sample of real result URLs fetched with plain HTTP and with Playwright, comparing extracted text length, would settle it.

## Proposed response shape (design sketch, from F10–F15)

One tool, `web_search`, with `query`, `tier` (`urls` | `summary` | `full`), `max_results`, and for `full` the paging parameters `max_length` and `start_index`. It returns JSON in a text block (F11, F14):

```json
{
  "query": "dotnet mcp server",
  "tier": "summary",
  "provider": "brave",
  "results": [
    {
      "rank": 1,
      "title": "…",
      "url": "https://…",
      "snippet": "…",
      "summary": "… (tier summary/full: excerpt or meta description, no LLM)",
      "content": "… (tier full: Markdown of the main article)",
      "content_length": 18234,
      "truncated": true,
      "next_start_index": 8000,
      "fetch_error": null
    }
  ]
}
```

Budget per tier (F12):
- `urls`: about 1–2k tokens.
- `summary`: stays under the 10k-token warning.
- `full`: pages through the content, with `anthropic/maxResultSizeChars` raised for this tool.

## Still open

- **Terms of service:** nobody has read Bing's or Brave's terms on automated querying. That decides whether Bing HTML scraping can be offered at all, even as an opt-in.
- **Scraping under load:** Bing and Brave HTML were checked with one request from a home IP. Repeated queries were never tested.
- **SearXNG in practice:** memory use, limiter behaviour on `format=json`, and which engines answer from a home IP are all untested.
- **Claude Code handling of typed output (F14):** test text-only against text plus `structuredContent` with the installed `claude` CLI before choosing.
- **Sampling (F15):** read issue #1785 and the end of the Claude Code MCP page.
- **SDK text block:** does the C# SDK 2.2.0 also emit the JSON text block when `UseStructuredContent` is on? A 10-line server would show it.
- **Extraction quality:** SmartReader and ReverseMarkdown have not been run on real result pages, so neither summary quality nor full-page quality is known.
- **Playwright's share of pages (F17):** the share of result pages that need JavaScript is unmeasured.
- **Tavily credits:** how many credits one search costs.
- **Caching rules:** the caching terms of Brave, Tavily, Exa and Mojeek. These matter if ai-hero-rat caches results.
- **Owner decisions:**
  - Which provider ships first: a keyed API (Brave, Tavily or Exa), SearXNG, or both?
  - Is a key requirement acceptable for a "local" tool?

## Plan additions

- **2026-10-14, owner:** the API keys for Brave, Exa and TinyFish go into the vinted-offers web project's secrets (`src/VintedOffers.Web` in the `vinted-offers` repo, user-secrets id `vinted-offers-7c1f4b52`). No key lands in a tracked file. `scripts/copy-search-secrets.sh` copies the `Search:ApiKey:*` entries from there into ai-hero-rat's own user secrets (`ai-hero-rat-c3af1c6c`), passing values only through a pipe and printing key names only. TinyFish was not covered by this research, so its pricing and terms are still unread.
