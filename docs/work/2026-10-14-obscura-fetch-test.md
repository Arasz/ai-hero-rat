# Research: Obscura as a local page fetcher on four real Polish pages

**Date:** 2026-10-14
**Question:** Can Obscura, run locally on this Mac, fetch a skalnik.pl product page, a Google results page, an allegro.pl product page and a vinted.pl item and return usable Markdown, how fast, and does `--stealth` change the outcome?

Method: one run per URL per mode with Obscura v0.2.4 (`obscura-aarch64-macos-stealth`), on a MacBook (macOS 27.0.1, arm64) on a residential connection, 2026-10-14 around 18:42 UTC. The command was `/usr/bin/time -l obscura fetch <url> --dump markdown --timeout 30 --quiet [--stealth]`. Each case ran once, so every time below is one sample, not a typical value. Outputs are in the session scratchpad and are not committed.

```chart:matrix
title: usable page content returned (1 yes, 0 no), one run each
site, plain, stealth
skalnik (Cloudflare), 0, 1
vinted, 1, 1
allegro (DataDome), 0, 0
google /search, 0, 0
```

## Findings

### F1 — The macOS release is two binaries totalling about 141 MiB, not one ~70 MiB binary [MEASURED]

The archive unpacks to `obscura` (75,397,152 bytes) and `obscura-worker` (72,034,928 bytes). The ~70 MiB figure in the README matches one binary, or the 67 MiB compressed archive. The README says to keep both together for `scrape`. A `fetch`-only integration might need only `obscura`, but nobody tested that.

**Evidence:** downloaded `https://github.com/h4ckf0r0day/obscura/releases/download/v0.2.4/obscura-aarch64-macos-stealth.tar.gz`. Its sha256 `183ce21f…a435a037` matched the digest from `gh api repos/h4ckf0r0day/obscura/releases/latest`. Ran `ls -la` after `tar xzf`.

### F2 — Vinted returned the full item in both modes [MEASURED]

Both runs returned the title "Xiami C200", the price 97,12 zł (104,88 zł with the Vinted fee), the condition, brand, colour, description and shipping cost: about 25 KB and 509–513 lines of Markdown. Plain mode took 22.35 s with a 371 MB peak RSS. Stealth took 10.22 s with 357 MB. Each mode ran once, so the time difference may be noise.

**Evidence:** `vinted-plain.md` and `vinted-stealth.md`, item block at about lines 107–138, plus the `/usr/bin/time -l` output for each run.

### F3 — Skalnik's Cloudflare check blocked plain mode and let stealth through [MEASURED]

Plain mode returned 466 bytes of Cloudflare's "Performing security verification … Waiting for www.skalnik.pl to respond" page in 5.33 s. Stealth mode returned 67,753 bytes in 8.67 s (244 MB peak RSS), with the product heading "SPODNIE PEDROC 2 DST 2/1 WOMEN-NAVY BLAZER DAMSKIE" and the prices 490.99 zł and 719.99 zł (−32%). That is one run per mode from one IP.

**Evidence:** `skalnik-plain.md` (the whole file) and `skalnik-stealth.md` lines 577–600.

### F4 — Allegro returned an empty page in both modes: a DataDome challenge [MEASURED]

The Markdown dump was 1 byte in plain mode (0.24 s) and in stealth mode (0.37 s). A separate plain `--dump html` run returned 1,611 bytes: a page titled `allegro.pl` whose script loads from `geo.captcha-delivery.com`, which is DataDome's challenge. The HTML for the stealth run was not captured.

**Evidence:** `allegro-plain.md`, `allegro-stealth.md`, `allegro-plain.html` (first 800 bytes inspected).

### F5 — Google answered every request with its "unusual traffic" CAPTCHA page [MEASURED]

The plain, stealth and `--obey-robots` runs each returned about 1.2 KB containing "Our systems have detected unusual traffic from your computer network", in 1.4–1.6 s. No results came back. The page shows the client's public IP, which this record leaves out.

**Evidence:** `google-plain.md`, `google-stealth.md`, `google-robots.txt`.

### F6 — `fetch` exits 0 whether it got the page or a block [MEASURED]

All ten `fetch` runs exited 0. That includes the empty Allegro dumps and every challenge page. A caller has to tell a block from real content by looking at the content.

**Evidence:** the `rc=` column of the run loop, plus the separate Allegro HTML and Google `--obey-robots` runs.

### F7 — `fetch --obey-robots` still fetched Google `/search`, which robots.txt disallows [MEASURED]

Google's robots.txt has `Disallow: /search` under `User-agent: *`. With `--obey-robots`, `obscura fetch` still requested the page and returned the CAPTCHA page (exit 0, 1.64 s). The README lists `--obey-robots` only among the `serve` flags, not the `fetch` flags. So the likely explanation is that `fetch` accepts the flag and ignores it. That explanation is INFERRED; `serve --obey-robots` was not tried.

**Evidence:** `curl -s https://www.google.com/robots.txt` lines 1–4, the `google-robots.txt` output, and the README's `obscura serve` and `obscura fetch` flag tables (read earlier this session).

### F8 — Peak memory was 37–371 MB, and about 30 MB only on blocked pages [MEASURED]

Peak RSS was 37–39 MB for the empty Allegro runs and 83–90 MB for the Google and Skalnik challenge pages. Pages with real content took 244–371 MB. The README's "30 MB" figure matched only a run that loaded nothing.

**Evidence:** the `maximum resident set size` line from `/usr/bin/time -l` for each run.

### F9 — The Markdown dump is the whole page, navigation included [MEASURED]

The Skalnik product heading appears at line 577 of 1,174. The Vinted item title appears at about line 107 of 509, after sign-up links and spacer lines. `--dump markdown` is not a main-article extraction.

**Evidence:** `grep -n` on `skalnik-stealth.md` and `vinted-stealth.md`.

### F10 — For ai-hero-rat, Obscura works as an optional fetcher that still needs block detection and article extraction [INFERRED]

This follows from F2–F9. Stealth got real content from 2 of 4 targets, the Cloudflare one included. The DataDome site and Google blocked both modes, and the exit code never says so (F6). The output still needs SmartReader or similar to cut it down to the article (F9). Peak memory is close to a headless Chromium's on real pages (F8). F7 means robots.txt compliance would have to be enforced by ai-hero-rat itself, not delegated to `fetch`.

## Still open

- **Repeatability:** each case ran once from one residential IP. Three runs per case, or a run from a datacenter IP, would show whether the Skalnik stealth pass and the Vinted times hold.
- **Comparison:** nobody fetched the same four URLs with plain `HttpClient`, Playwright Chromium or TinyFish `fetch_content`. Without that, Obscura's 2 of 4 has nothing to be measured against. That comparison is the F17 measurement in `2026-10-14-search-backend-options.md`.
- **`serve --obey-robots`:** whether the CDP server mode enforces robots.txt (F7) is untested.
- **`obscura` alone:** whether `fetch` runs without `obscura-worker` next to it (F1) is untested.
- **Allegro in stealth mode:** only the plain HTML was captured. That stealth also hit DataDome is INFERRED from its empty Markdown.
- **Terms:** nobody read Allegro's, Skalnik's or Vinted's terms on automated access, and stealth mode exists to defeat bot checks. Whether ai-hero-rat should ship it at all is a policy question this test does not answer.
