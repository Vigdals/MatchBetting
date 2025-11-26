# MatchBetting

**MatchBetting** er ein ASP.NET Core MVC-applikasjon som hentar kampdata frå
NIFS API og lar brukarar logge inn, tippe H/U/B, legge inn sidebets og sjå
leaderboard basert på faktiske resultat.  
Prosjektet starta som ei EM 2024-løysing og er no oppgradert til
**FIFA World Cup 2026**. Målet er å gjere det heilt gjenbrukbart i framtida.

---

## Korleis systemet fungerer

### 1. Hente turnering og kampar

`HomeController.Index()` gjer følgjande:

1. Hentar alle “stages” i turneringa frå NIFS  
2. Filtrerer på rett år (2026)  
3. Hentar alle kampar for kvar stage  
4. Mapper desse til EF-modellen `Match` og lagrar/oppdaterer i databasen  
5. Returnerer ferdige `NifsKampViewModel`-objekt til UI

### 2. Visning og tipslegging

Views ligg under `Views/Home/`:

- **Index** → liste over kampar + val for H/U/B  
- **LeaderBoard** → poeng for alle brukarar  
- **Historikk** → tidlegare kampar  
- **SideBets** → toppscorar, vinnarlag, kort m.m.

### 3. Poengsystem

- 1 poeng for rett utfall (H/B/U)  
- 0 poeng dersom feil  
- Ein kan ikkje tippe innan **2 timar** før kampstart

Resultat blir bestemt slik:

```csharp
homeScore90 > awayScore90 = H
homeScore90 < awayScore90 = B
else = U
```

---

## Konfig

Prosjektet brukar hardkoda verdiar for å styre kva turnering som er aktiv.

### 1. Turnering-ID

I `HomeController.cs`:

```csharp
private readonly string TournamentID = "56";
```

- `56` = World Cup 2026  
- `59` = Euro 2024

### 2. Årsfilter

I `NifsApiService`:

```csharp
if (gruppe.yearStart == 2026)
```

Dette sikrar at du berre får gruppene knytte til rett turnering.

---

## Køyr prosjektet lokalt

### 1. Klon repoet

```
git clone https://github.com/Vigdals/MatchBetting
```

### 2. Opne i Visual Studio

### 3. Opprett `appsettings.json`

Prosjektet treng ein connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MatchBetting;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Migrer databasen

Opne Package Manager Console:

```powershell
Update-Database
```

Dersom modellane blir endra seinare:

```powershell
Add-Migration <Namn>
Update-Database
```

### 5. Køyr prosjektet

Start **IIS Express** frå Visual Studio.  
Repoet er konfigurert til HTTPS på port **44303**.

---

## Lisens

MIT — bruk koden slik du vil.

---

## Kontakt

**@Vigdals** på GitHub.