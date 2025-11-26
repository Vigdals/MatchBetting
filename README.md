# MatchBetting

**MatchBetting** er ein ASP.NET Core MVC-applikasjon som hentar kampdata frå
NIFS API og let brukarar logge inn, tippe H/U/B, legge inn sidebets og sjå
leaderboard basert på faktiske resultat.  
Prosjektet starta som ei EM 2024-løysing og er no oppgradert til
**FIFA World Cup 2026**. Målet er å gjere det så gjenbrukbart som mogleg for framtidige turneringar og grupper.

---

## Korleis systemet fungerer

- Brukarinnlogging og -handsaming via **ASP.NET Core Identity**
- Henting av kampdata frå **NIFS API** (turnering, grupper/stages, kampar)
- Tipping på kamputfall (H/U/B) per brukar
- Sidebets (t.d. toppscorar, vinnarlag, kort o.l.)
- Leaderboard med samla poeng per brukar
- Historikk-visning for ferdigspel­te kampar
- Designa for å kunne støtte fleire grupper/konkurransar (t.d. *Tippekonk*, *Digi*, *Luster FPL*)

---

## Arkitektur og mappestruktur

Prosjektet er bygd som ein klassisk ASP.NET Core MVC-app med EF Core:

- `Controllers/` – MVC-controllerar som koplar HTTP-requestar til logikk og views  
  (t.d. controllerar for startside, tipping, leaderboard og sidebets).
- `Views/` – Razor-views for startsida, leaderboard, historikk, sidebets m.m.
- `Models/` – domeneobjekt for EF Core (kamp, tips, sidebets, brukar m.m.).
- `NifsModels/` – DTO-ar som speglar JSON-strukturen frå NIFS API.
- `ViewModels/` – tilpassa modeller for UI (kombinerer data frå fleire kjelder til éin modell per view).
- `Service/` – tenestelag med t.d. `NifsApiService` som kallar NIFS API og mappar resultat til interne modellar.
- `Data/` – `ApplicationDbContext` og kopling mot databasen.
- `Migrations/` – EF Core-migrasjonar for å halde databasen i synk med C#-modellane.
- `Utils/` – hjelpeklassar, utrekningar og diverse støttefunksjonar.
- `wwwroot/` – statiske filer (CSS, JS, bilete).

`Program.cs` set opp DI-container, Identity, DbContext, routing og HTTPS.

---

## Datastruktur og domenemodell

Oversiktsnivå (klasse- og eigedomsnamn kan variere litt i koden, men konsepta er slik):

- **Kamp (`Match`)**
  - Representerer éin fotballkamp
  - Har mellom anna: referanse til NIFS-ID, dato/tid, lag, resultat etter 90 minutt, om kampen er ferdig m.m.
- **Tips (`Bet`)**
  - Knytt til både `Match` og `ApplicationUser`
  - Lagrar kva brukar har tippa (H/U/B) og poeng gitt for denne kampen
- **Sidebets**
  - Eigne tabellar for t.d. toppscorar, vinnarlag, kort m.m.
  - Brukast til konkurransar ved sida av vanleg kamp-tipping
- **Brukar (`ApplicationUser`)**
  - Identity-brukar som loggar inn
  - Utvidbar med ekstra felt som t.d. gruppetilhøyrigheit (planlagt, sjå under *Planlagde features*)

Databasen blir styrt via EF Core og migrasjonar, slik at du kan vidareutvikle modellen utan å miste kontroll på schema-endringar.

---

## Konfig

1. Applikasjonen hentar alle *stages* (grupper/og vidare) i turneringa frå NIFS.
2. Det blir filtrert på rett år (per i dag 2026).
3. For kvar stage blir alle kampar henta.
4. Kampane blir mappa til EF-modellen `Match` og lagra/oppdatert i databasen.
5. Til slutt blir det returnert view-modellar (t.d. `NifsKampViewModel`) til UI-et.

### 2. Visning og tipping

Typisk brukarflow:

1. Brukar registrerer seg og loggar inn via Identity.
2. På startsida får brukaren ei liste over kommande og pågåande kampar, med moglegheit for å tippe H/U/B.
3. Eigne sider viser:
   - **Leaderboard** – poeng per brukar
   - **Historikk** – ferdigspelte kampar med resultat og treff/ikkje treff på tips
   - **SideBets** – oversikt over sidekonkurransar

### 3. Poengsystem

- 1 poeng for rett utfall (H/B/U)
- 0 poeng dersom feil
- Ein kan ikkje tippe seinare enn **2 timar før kampstart**

Utfallet blir bestemt ut frå resultat etter 90 minutt:

```csharp
homeScore90 > awayScore90 = H
homeScore90 < awayScore90 = B
else = U
``` 


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