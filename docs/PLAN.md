# SatPrep: build plan (v1.0, "ready to carry downstairs")

Goal: a complete offline SAT study platform for the air-gapped Windows Server
2022 machine. It should match Aniko and OnePrep on everything except AI. It
installs as a Windows Service with one script and runs with no internet.

## 1. Content strategy (decided 2026-11-22)

We are **not** asking Aniko/OnePrep for anything more, and we do not
bypass their bot protection (Aniko: Vercel challenge; OnePrep: robots.txt blocks
Claude crawlers). v1.0 therefore ships with **its own original question bank**,
and makes adding more content trivial:

| Source | How | Volume in v1.0 |
|---|---|---|
| Math generators (`tools/SatPrep.ContentGen`) | Parameterized templates per skill, answers **computed** (always correct), distractors built from common mistakes, worked explanation generated from the same numbers. Fixed seed, so the output is stable and committed as a pack. | ~800 (all 19 math skills × 3 difficulties) |
| R&W generators | Templates for Boundaries, Form/Structure/Sense, Transitions, Words in Context (built from the vocab bank), Rhetorical Synthesis (note sets). | ~400 |
| Hand-written R&W passages | Central Ideas, Inferences, Command of Evidence, Text Structure & Purpose, Cross-Text Connections. | ~60 |
| Vocabulary bank | Word, part of speech, definition, example sentence. | ~250 words |
| Lessons | One lesson per domain with a section per skill: strategy, worked example, common traps. | 8 lessons / 29 skill sections |
| **Owner-added** | Content page: upload JSON packs, plus an in-app question editor. Saved pages from sites we have permission for can be converted into packs later. | unlimited |

## 2. Feature list (v1.0; scope cut by owner 2026-11-22)

**Focus:** questions, skill progress, daily plan, practice by topic,
explanations, studying. **Dropped:** passwords/accounts admin,
leaderboard, streak competitions, analytics extras.

### Core
1. **Enter your name** start screen: pick an existing learner or type a new name. No passwords. Everything is remembered per name (cookie keeps you signed in on that PC).
2. **Home / daily plan**: today's tasks (review due, weakest skills, a lesson to read, vocab, the weekly practice test), days until the test, estimated score versus target.
3. **Skill progress**: all 29 skills with mastery band, %, accuracy, attempts, last practiced; per-domain summary; "practice" and "study" buttons on every skill.
4. **Practice by topic**: choose a section, domain or skill, difficulty (Easy/Medium/Hard/**Adaptive**) and pool (new / all / incorrect / marked / review due). One question at a time, then instant feedback, the **full explanation** and the mastery change.
5. **Question bank browser**: filter by skill, difficulty and status (unseen/correct/incorrect/marked) and open any question.
6. **Mistakes**: every missed question, with retry; misses come back automatically after 1, 3 and 7 days (spaced review).
7. **Studying**: lessons per domain, with a section per skill (strategy, worked examples, traps) and a "practice this skill" link; vocabulary flashcards.
8. **Practice tests** (from the original request): full adaptive test, or section-only. Timer, question navigator, mark for review, answer eliminator, calculator and reference sheet, then results with section scores, per-domain accuracy and question review with explanations.

### Content management (anyone; no admin role)
9. Import JSON question packs (bundled folder imported on start-up, plus upload in the browser), simple question editor, retire a bad question.

### Platform / deployment
10. Windows Service built in (`UseWindowsService`); `Install-SatPrep.ps1` installs to `C:\SatPrep`, registers the **SatPrep** service (visible in Server Manager → Tools → Services; automatic start, restart on failure), opens firewall TCP 5100, health-checks. Plus `Update-SatPrep.ps1` (DB backup first), `Uninstall-SatPrep.ps1` and `Backup-SatPrep.ps1`.
11. EF Core migrations with an automatic DB backup before upgrading.
12. Zero external requests (verified in an end-to-end test).

## 3. Architecture

- ASP.NET Core 8 Razor Pages, EF Core 9 SQLite, cookie auth, as in ShiftManager.
- Server-rendered pages. The test player and calculator are vanilla JS classes using small JSON endpoints.
- "Login" = cookie holding the learner id; no passwords.
- Services (pure where possible, unit-tested): `MasteryService`, `ScoreEstimator`, `TestAssembler`, `PracticeSelector`, `ReviewScheduler`, `StudyPlanner`, `ContentImporter`.
- Content as data: `Content/packs/*.json`, `Content/lessons/*.md`, `Content/vocab.json`.

## 4. Score estimation (documented approximation)

- Each section: weighted raw = sum of difficulty weights of correct answers (E=1, M=1.5, H=2).
- The routed module caps the range: lower module 2 → max ≈ 600, upper module 2 → up to 800. The weighted fraction is mapped to the 200–800 scale.
- Dashboard estimate (no test needed): per section, the domain-weighted mean of the mastery-model P(correct on a typical item), mapped the same way.
- Always labeled "estimate". The official SAT uses IRT scoring that isn't public.

## 5. Milestones

1. Data model + name sign-in + layout
2. Practice engine, explanations, skill progress, bank browser, mistakes/review
3. Original content (math + R&W generators, passages, vocab, lessons)
4. Daily plan + lessons + vocab
5. Practice tests (assembler, player, calculator, reference sheet, results)
6. Content import/editor
7. Windows Service installer + release build
8. End-to-end verification, docs, release zip

## 6. Status (2026-11-22): v1.0 complete

All features in section 2 are built and verified:
- 22 unit/service tests (grading, mastery, importer, full adaptive test: perfect run = 1600, blank module routes to the easier module 2)
- Playwright smoke test (`tools/e2e/smoke.js`): 25/25 checks covering sign-in, plan, practice, mistakes, bank, lessons, vocab, short test with calculator, content page, phone layout, **zero JS errors and zero external requests**
- Release zip `SatPrep-v1.0.0-win-x64.zip` (self-contained, about 48 MB) with the Windows Service installer. Checksums and installer scripts were parse-checked; **the installer itself still needs a first run on the real Windows server.**

Open items:
- Import College Board / Aniko / OnePrep content once it is available as an export (see CONTENT_SOURCES.md).
- Hebrew UI (if wanted), following ShiftManager's localization approach.
