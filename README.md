# MatchBetting

**MatchBetting** er ein ASP.NET Core MVC-applikasjon som hentar kampdata frå
NIFS API og lar brukarar logge inn, tippe H/U/B, legge inn sidebets og sjå
leaderboard basert på faktiske resultater.\
Prosjektet starta som ein EM 2024-løsning og er no oppgraddert til
**FIFA World Cup 2026**. Håpar å gjera det heilt gjenbrukbart i framtida.

## 📦 Kjør prosjektet lokalt

### 1. Klon repoet

    git clone https://github.com/Vigdals/MatchBetting

### 2. Åpne i Visual Studio 2022

### 3. Opprett `appsettings.json`

Prosjektet trenger en connection string:

``` json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MatchBetting;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Migrer databasen

Åpne Package Manager Console:

``` powershell
Update-Database
```

Hvis modeller endres senere:

``` powershell
Add-Migration <Navn>
Update-Database
```

### 5. Kjør prosjektet

Start **IIS Express** fra Visual Studio.\
Repoet er konfigurert til HTTPS på port **44303**.

------------------------------------------------------------------------

## ⚽ Hvordan systemet fungerer

### 1. Hente turnering og kamper

`HomeController.Index()` gjør følgende:

1.  Henter alle "stages" i turneringen fra NIFS\
2.  Filtrerer på riktig år (2026)\
3.  Henter alle kamper for hver stage\
4.  Mapper disse til EF-modellen `Match` og lagrer/oppdaterer i
    databasen\
5.  Returnerer ferdige `NifsKampViewModel`-objekter til UI

### 2. Visning og tipslegging

Views ligger under `Views/Home/`:

-   **Index** → liste av kamper + valg for H/U/B
-   **LeaderBoard** → poeng for alle brukere
-   **Historikk** → tidligere kamper
-   **SideBets** → toppscorer, vinnerlag, kort m.m.

### 3. Poengsystem

-   1 poeng for korrekt utfall (H/B/U)
-   0 poeng hvis feil
-   Kan ikke tippe innen **2 timer** før kampstart

Resultat bestemmes av:

``` csharp
homeScore90 > awayScore90 = H
homeScore90 < awayScore90 = B
else = U
```

------------------------------------------------------------------------

## 🧩 Turnering & konfigurasjon

Prosjektet bruker hardkodede verdier for å avgjøre hvilken turnering som
brukes.

### 1. Turnering-ID

I `HomeController.cs`:

``` csharp
private readonly string TournamentID = "56";
```

-   `56` = World Cup 2026\
-   `59` = Euro 2024

### 2. Årsfilter

I `NifsApiService`:

``` csharp
if (gruppe.yearStart == 2026)
```

Dette sikrer at du kun får gruppene for riktig turnering.

------------------------------------------------------------------------

## 🗂 Prosjektstruktur

    MatchBetting/
    │
    ├── Controllers/
    │   └── HomeController.cs
    │
    ├── Data/
    │   └── ApplicationDbContext.cs
    │
    ├── Models/
    │   ├── Match.cs
    │   ├── MatchBetting.cs
    │   ├── SideBet.cs
    │   └── Log.cs
    │
    ├── NifsModels/
    │   └── Modeller brukt for JSON-deserialisering av NIFS-API
    │
    ├── Service/
    │   ├── INifsApiService.cs
    │   └── NifsApiService.cs
    │
    ├── Utils/
    │   ├── ApiCall.cs
    │   └── Custom DateTime Converters
    │
    ├── Views/
    │   └── Razor Views for kamper, leaderboard og sidebets
    │
    └── appsettings.json  (ikke inkludert i repo)

------------------------------------------------------------------------


## 🔧 Anbefalte forbedringer

-   Flytte turnerings-ID og årstall til `appsettings.json`
-   Erstatte `.Result` med `await` i hele koden (fjerner deadlocks)
-   Lage background-job (Hangfire eller HostedService) for periodisk
    NIFS-oppdatering
-   Rydde gamle EM-views og gjøre layout mer modulær

------------------------------------------------------------------------

## 📄 Lisens

MIT -- bruk koden som du vil.

------------------------------------------------------------------------

## 👤 Kontakt

**@Vigdals** på GitHub.
