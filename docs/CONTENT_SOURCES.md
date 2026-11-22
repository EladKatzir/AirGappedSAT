# Content sources & licensing

**The app is only as good as its question bank, and we can't just copy Aniko's.**

## Aniko (aniko.ai): free to *use*, not free to *copy*

Aniko's question bank and courses are free to access on their site, but their
Terms of Service (updated 2026-10-29) forbid taking the content out:

- §6: you may not "Scrape, copy, reverse engineer, resell, or redistribute Aniko content or any part of the service, except as expressly allowed."
- §14: "You may not copy, modify, distribute, sell, or create derivative works from Aniko except as expressly allowed." It lists "the question bank, study plans, explanations…" as owned by San Junipero Ltd. The licence granted is a "personal … non-transferable right to use Aniko for your own SAT preparation."
- Their pages also use anti-scraping protection.

So scraping Aniko into SatPrep would breach their terms. **Option:** email
hello@aniko.ai and ask for written permission for closed, non-commercial,
internal use. "Except as expressly allowed" leaves room for that. If they
grant it, record the permission here and put its reference in each pack's
`licence` field.

## OnePrep: same answer

Their terms ban scrapers, copying and derivative works ("reproduce, distribute,
host…", "copy, distribute, reproduce, alter… or create derivative works of any
Content"). Not usable without permission.

## College Board (official): best realistic source, but check the terms

- **Official paper practice tests**: SAT Practice Tests 4–11 (digital-format PDFs with answer explanations) are free downloads. The site says "anyone is welcome to use our downloadable paper practice tests." These are real SAT questions with official explanations, and they map directly onto our full-length test feature.
- **Educator Question Bank**: thousands of official questions, filterable by domain, skill and difficulty, with export for teachers.
- **Open issue:** we did not find an explicit licence for re-hosting these inside another app. Before importing, get a clear read of College Board's terms (or ask them) for closed, non-commercial internal use. It is copyrighted material.

## Original content: always safe

We write our own questions against the official taxonomy (the sample pack is
this). This is slow by hand, but explanations and question variants can be
drafted in bulk with an LLM *outside* the air gap, then reviewed by a person
and shipped as packs. We fully own the result.

## Decision log

| Date | Decision |
|---|---|
| 2026-11-22 | Do not scrape Aniko/OnePrep (ToS). Pipeline built for licensed / original packs. Source choice pending. |
| 2026-11-22 | Owner reports permission from **Aniko** and **OnePrep** to use their content for personal study on the air-gapped network ("own SAT preparation"). **Action: save the written replies under `docs/permissions/`.** Scope = personal use by the owner; widen only with new permission. |
| 2026-11-22 | Automated download blocked on both sites: Aniko question pages sit behind a Vercel bot challenge (HTTP 429 `x-vercel-mitigated: challenge`); OnePrep's robots.txt disallows `ClaudeBot`/`anthropic-ai`. We do not bypass either. Paths forward: (a) ask each site for a data export (JSON/CSV), or (b) owner saves pages while browsing normally, and `tools/` parsers convert saved HTML into packs. |
