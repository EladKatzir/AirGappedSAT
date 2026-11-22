# SatPrep

Offline SAT study platform for an **air-gapped** Windows Server 2022 network.
Sister project of ShiftManager (github.com/EladKatzir/shiftmanager) and uses the same
hosting model.

## Hard rules
- **No internet at runtime.** No CDNs, no external fonts/APIs, no telemetry. Every JS/CSS/font lives in `src/SatPrep/wwwroot/lib`. (Desmos is not available; it needs internet.)
- Release is a **self-contained win-x64 folder/zip** run via `START_HERE.bat`. The target has no .NET installed.
- Keep NuGet versions aligned with ShiftManager where possible (shared offline cache).
- Never import third-party question content without a licence. See `docs/CONTENT_SOURCES.md`.
- Code style (owner preference): class-based, thin `Main`, every class/method/variable commented with its purpose.

## Layout
- `src/SatPrep`: ASP.NET Core 8 Razor Pages + EF Core SQLite (`satprep.db`)
  - `Data/SatTaxonomy.cs`: official 8 domains / 29 skills (codes are stable keys)
  - `Services/MasteryService.cs`: Elo/IRT skill mastery (pure, unit-tested)
  - `Services/ContentImporter.cs`: imports `Content/packs/*.json` on start-up
- `tests/SatPrep.Tests`: xUnit
- `build/build-release.sh <ver>` (Linux/cloud) or `build/Build-Release.ps1 -Version <ver>` → `packages/SatPrep-v<ver>-win-x64.zip`
- `deploy/`: files copied into the release (START_HERE.bat, README.txt)
- `docs/PLAN.md`: feature plan and roadmap

## Commands
- `dotnet test`
- `dotnet run --project src/SatPrep` → http://localhost:5100
- `build/build-release.sh 0.1.0`
