# MatchBetting

Small ASP.NET Core Razor Pages project that fetches match data from the NIFS API and provides simple user betting, side-bets and leaderboards. This repo was updated from UEFA Euro 2024 -> FIFA World Cup 2026.

---

## Quick facts
- Framework: .NET 8
- C# language version: 12
- Pattern: Razor Pages with some MVC Controllers (controllers live in `Controllers\`)
- DB: EF Core (Identity + app models)
- External API: NIFS (api.nifs.no)

---

## How to run locally

1. Open solution in Visual Studio 2022.
2. Restore packages: use __Manage NuGet Packages__ or run dotnet restore.
3. Build: __Build Solution__.
4. Run and debug: __Start Debugging__ or __Start Without Debugging__.
5. If DB is missing, create migrations / update DB:
   - Open Package Manager Console and run:
     - `__Add-Migration__ <Name>` (if you change models)
     - `__Update-Database__`

---

## Important files & responsibilities

- `Controllers\HomeController.cs`
  - Contains the tournament ID currently used by the UI.
  - Field: `private readonly string TournamentID = "56";`
  - Change this ID to switch tournament feed (e.g., Euro 2024 used `59`; World Cup 2026 uses `56`).

- `Service\INifsApiService.cs`
  - Filters the stages by `yearStart`. Current code checks for `2026`:
    - `if (gruppe.yearStart == 2026) { ... }`
  - Update this year when switching tournaments, or follow the refactor below to avoid manual edits.

- `Utils\ApiCall.cs`
  - Contains helper to call NIFS endpoints. Inspect / modify headers / timeouts here.

- `Views\Home\Index.cshtml`, other Razor Views
  - Presentation layer for bet placement and leaderboard.

- `appsettings.json`
  - Place environment-specific configuration here (connection strings, rising settings).

---

## Where to update tournament/year (manual steps)

If you prefer the quick manual update:

1. Tournament ID
   - Edit `Controllers\HomeController.cs` and change:
     - `private readonly string TournamentID = "56";`
   - Use `59` to point back to Euro 2024.

2. Year filter
   - Edit `Service\INifsApiService.cs` and change:
     - `if (gruppe.yearStart == 2026) { ... }`
   - For Euro 2024 use `2024`.

Note: In this repository the values are already set for World Cup 2026:
- `HomeController` TournamentID is `"56"`.
- `INifsApiService` filters on `gruppe.yearStart == 2026`.

---

## Recommended small refactor (to avoid future manual edits)

Move tournament id and year into configuration to make future updates trivial.

Example `appsettings.json` additions:
