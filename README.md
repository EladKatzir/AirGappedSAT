# SatPrep (AirGappedSAT)

An offline SAT study platform for an air-gapped Windows Server 2022 network. Sister project of ShiftManager, with the same hosting model: a self-contained Windows folder, a SQLite database file, and no internet.

## What learners get

- **Enter your name**, no passwords. Progress, skills and answers are saved per name.
- **Today**: a daily study plan built from your test date, goal and weakest skills, plus a score estimate, streak and spaced reviews that are due.
- **Practice by topic**: pick any section, domain or one of the 29 official skills. Choose Easy/Medium/Hard or **Adaptive** (targets about 60–75% success). Every question shows a full explanation and how your mastery changed.
- **Skill progress**: mastery for every skill (Elo/IRT model). "Mastered" requires hard questions answered correctly, not just volume.
- **Mistakes**: every missed question. Misses come back automatically after 1, 3 and 7 days.
- **Question bank**: browse and filter everything (unseen / right / wrong / marked).
- **Study**: lessons for all 8 domains (strategy, worked examples, traps) and vocabulary flashcards using a Leitner box system.
- **Practice tests**: full-length adaptive (module 2 is harder or easier depending on module 1), section-only, or a short test. Bluebook-style player with timer, navigator, mark for review, answer eliminator, highlighter, an **offline graphing calculator** (replaces Desmos) and the reference sheet. Results include domain accuracy, a pacing chart and a full review.
- **Content page**: upload JSON question packs, edit or add questions, retire bad ones, see real accuracy per question, download a database backup.

Bundled content is original: **1,182 questions** (math answers are computed by generators, so they are always correct), 210 vocabulary words and 8 lessons. See `docs/CONTENT_SOURCES.md`.

## Install on the server

1. Build or download `packages/SatPrep-v1.0.0-win-x64.zip` and copy it over by USB.
2. Unblock and extract it, then double-click **`INSTALL.bat`**.
3. Open `http://localhost:5100`, or `http://SERVER-NAME:5100` from other machines.

The installer registers the **"SatPrep - SAT practice"** Windows Service (Server Manager → Tools → Services) and opens the firewall. Full instructions, including upgrade, backup and uninstall, are in [`deploy/README.txt`](deploy/README.txt).

## Develop

```bash
dotnet test
```

```bash
dotnet run --project src/SatPrep
```

```bash
build/build-release.sh 1.0.0
```

```bash
node tools/e2e/smoke.js http://localhost:5100
```

```bash
python3 tools/contentgen/build_packs.py
```

The commands above: run the unit and service tests (22), run the app at http://localhost:5100, build the Windows release zip (on Windows use `build\Build-Release.ps1 -Version 1.0.0`), run the browser smoke test against a running app, and regenerate the bundled content packs.

Stack: ASP.NET Core 8 Razor Pages, EF Core 9 + SQLite, Bootstrap 5.3, KaTeX, math.js and Chart.js. Everything is vendored in `wwwroot/lib`.
