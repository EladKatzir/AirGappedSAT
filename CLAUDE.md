# SatPrep

Offline SAT study platform for an **air-gapped** Windows Server 2022 network.
Sister project of ShiftManager (github.com/EladKatzir/shiftmanager) and uses the same hosting model.

## Hard rules
- **No internet at runtime.** No CDNs, external fonts, APIs or telemetry. Every JS/CSS/font lives in `src/SatPrep/wwwroot/lib`. (Desmos is not available; `wwwroot/js/calculator.js` replaces it.)
- Release is a **self-contained win-x64 zip**, installed as a Windows Service by `deploy/INSTALL.bat`. The target has no .NET installed.
- Keep NuGet versions aligned with ShiftManager where possible (shared offline cache).
- Only import question content we have the right to use. See `docs/CONTENT_SOURCES.md`.
- Owner's scope decisions: **name-only sign-in (no passwords), no leaderboard, no admin roles**. Focus is questions, skill progress, daily plan, practice by topic, explanations and studying.
- Code style (owner preference): class-based, thin `Main`, every class/method/variable commented with its purpose.

## Layout
- `src/SatPrep`: ASP.NET Core 8 Razor Pages + EF Core SQLite (`satprep.db` next to the exe)
  - `Data/SatTaxonomy.cs`: official 8 domains / 29 skills (codes are stable keys)
  - `Data/Migrations`: EF migrations (add one for every model change: `dotnet ef migrations add Name -o Data/Migrations`)
  - `Services/`: `MasteryService` (Elo/IRT), `PracticeSelector`, `ProgressService` (records every answer), `TestService` (adaptive tests), `ScoreEstimator`, `StudyPlanner`, `ContentImporter`, `LessonService` (+ `Md` Markdown/KaTeX renderer)
  - `Content/packs/*.json` (questions), `Content/lessons/*.md`, `Content/vocab.json`, all imported or loaded at start-up
  - `wwwroot/js/test-player.js`, `wwwroot/js/calculator.js`
- `tests/SatPrep.Tests`: xUnit (grading rules, mastery, importer, full adaptive test flow)
- `tools/contentgen`: Python generators for the bundled content (`build_packs.py`)
- `tools/e2e/smoke.js`: Playwright end-to-end smoke test (fails on JS errors or any external request)
- `deploy/`: Install / Update / Uninstall / Backup PowerShell scripts, INSTALL.bat, START_HERE.bat, README.txt
- `build/build-release.sh <ver>` or `build/Build-Release.ps1 -Version <ver>` produces `packages/SatPrep-v<ver>-win-x64.zip`

## Commands
- `dotnet test`
- `dotnet run --project src/SatPrep` serves http://localhost:5100
- `node tools/e2e/smoke.js` (with the app running)
- `build/build-release.sh 1.0.0`

## Content conventions
Math in content is written as `$...$`. Markdig turns it into `\( \)` spans and KaTeX renders those (plain `$` is not a delimiter). Write money as `\$5`.
