# SatPrep: feature plan

Goal: an offline SAT study platform on the air-gapped network, modeled on
Aniko and OnePrep, without the AI features. It runs on the second Windows Server 2022 box,
next to ShiftManager.

## Reference sites: what they offer (researched 2026-11-22)

| Feature | Aniko | OnePrep | SatPrep? |
|---|---|---|---|
| Question bank by section, domain and skill | yes (4,000 free) | yes (4,000+ free) | **MVP** |
| Difficulty filter (Easy/Medium/Hard) | labels only | yes | **MVP** |
| Full step-by-step explanations | yes | yes | **MVP** |
| Filter: incorrect / marked for review | via Error Log | yes | **MVP** |
| Error log / mistakes review | yes (filter by difficulty, domain, skill) | yes | **MVP** |
| Skill mastery tracking | mentioned, formula not public | progress and accuracy per skill | **MVP** (ours: Elo/IRT, see below) |
| Full-length adaptive practice test (2 sections × 2 modules, M2 adapts to M1) | yes | yes (12 tests) | **Phase 2** |
| Section-only / half / short tests | yes | no | Phase 2 |
| Test UI: timer, mark for review, highlight, answer eliminator, reference sheet | yes | yes | Phase 2 |
| Graphing calculator | Desmos | Desmos | Phase 2: **Desmos needs internet**, so we bundle an open-source alternative |
| Results page: score 400–1600, per-domain accuracy, timing vs. pace | yes | yes | Phase 2 |
| Score estimate that updates as you practice | yes | yes | Phase 2 |
| Pacing analytics (seconds/question, too fast/slow) | yes | "time share by difficulty" | Phase 2 |
| Courses / lessons per domain | yes (free) | Masterclass video (paid) | Phase 3 (content dependent) |
| Study plan (test date, target score, then daily sessions) | yes | yes | Phase 3 |
| Vocabulary list, flashcards, quiz | yes (312 words) | yes | Phase 3 |
| Review Labs (replay misses by topic) | yes | no | Phase 3 (cheap once Error Log exists) |
| Bookmarks / collections | yes | no | Phase 3 |
| Leagues / leaderboards / streaks | yes (Lock-In) | no | Phase 4 (fun for a group) |
| AI tutor | yes | yes | **Out of scope** |
| Score guarantee, pricing | yes | yes | Out of scope |

## Extras that make sense for us ("and more")

- **Instructor/admin view**: see every learner's mastery heat map and assign skill sets. Neither site has this, and it fits a shared, closed network well.
- **Content admin**: import packs, retire bad questions, see question stats (actual % correct, which lets us re-calibrate difficulty from real data).
- **Spaced repetition**: missed questions come back after 1, 3 and 7 days.
- **Hebrew UI and RTL**: reuse ShiftManager's localization approach if learners want it. The content stays in English, like the real SAT.
- **Printable worksheets**: export a skill set to PDF for paper practice.

## Skill mastery model (built: `Services/MasteryService.cs`)

- Each learner has an ability rating per skill. Question difficulty maps to b = −1/0/+1.
- After each answer: `rating += K × (correct − P(correct))`, with K shrinking as evidence grows.
- Shown as 0–100% (expected accuracy on a Medium question) and as bands: Not started → Learning → Familiar → Proficient → **Mastered**.
- **Mastered** requires ≥85%, ≥8 attempts, and ≥2 correct Hard questions, so lots of easy wins can't fake it.
- `RecommendedDifficulty()` serves questions in the ~60–75% success zone, which gives adaptive practice for free.

## Roadmap (fastest path to something people use)

1. **MVP (practice loop)**: login (cookie auth copied from ShiftManager), practice by skill and difficulty, answer, explanation, mastery update, mastery dashboard, error log. *Most of the data model is already built.*
2. **Practice tests**: test assembly (by blueprint: 27+27 RW, 22+22 Math, M2 easier/harder routing), timed test player, results, score estimate. Needs ~98+ questions per test drawn from the bank.
3. **Learning**: lessons per domain, study plan, vocab and flashcards, review labs.
4. **Group features**: instructor dashboard, leaderboards.

## Status of the scaffold

- [x] ASP.NET Core 8 Razor Pages + EF Core SQLite (same stack and versions as ShiftManager)
- [x] Official taxonomy: 8 domains / 29 skills seeded
- [x] Question entity + JSON content-pack importer (idempotent, validated)
- [x] Mastery engine + unit tests
- [x] KaTeX bundled locally; verified zero external requests
- [x] Self-contained win-x64 release zip, buildable from Linux (cloud) or Windows
- [ ] Switch `EnsureCreated` to EF Migrations before the first real deployment
- [ ] Auth, practice flow, dashboard (MVP)
- [ ] Offline graphing calculator (candidates: function-plot (MIT), JSXGraph; check licence)
