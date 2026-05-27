# MatchBetting

**MatchBetting** er ein ASP.NET Core MVC-applikasjon for tippekonk under FIFA World Cup 2026.

Appen hentar kampdata frå NIFS API, lar brukarar registrere seg, velje gruppe, tippe H/U/B, legge inn sidebets og sjå leaderboard basert på faktiske resultat.

Prosjektet starta som ei EM 2024-løysing og er no oppgradert til VM 2026.

---

## Funksjonalitet

- Innlogging og registrering med ASP.NET Core Identity
- Brukarar kan velje konkurransegruppe ved registrering
- Leaderboard er filtrert per konkurransegruppe
- Kampdata blir henta frå NIFS API
- Kampar blir lagra og oppdatert i databasen
- Brukarar kan tippe H/U/B på kvar kamp
- Tipping blir låst 2 timar før kampstart
- Sidebets:
  - toppscorar
  - vinnarlag
  - spelar med flest kort
- Sidebets blir låst 2 timar før første kamp
- Sidebets blir synlege for gruppa etter fristen
- Admin-brukarar kan sjå sidebets før fristen
- Historikk viser låste/tidlegare VM 2026-kampar
- Autocomplete for spelarar og lag
- Adminside for enkel drift:
  - sjå brukarar
  - endre gruppe
  - slette brukarar
  - resette passord
  - seede spelarar frå NIFS
  - legge til spelarar manuelt
  - rydde brukarar utan e-post
  - normalisere sidebets

---

## Poengsystem

- 1 poeng for rett H/U/B per kamp
- 3 poeng for korrekt toppscorar
- 3 poeng for korrekt vinnarlag
- 3 poeng for korrekt spelar med flest kort

Kampresultat blir rekna etter 90 minutt:

```csharp
homeScore90 > awayScore90 = H
homeScore90 < awayScore90 = B
else = U
```

Ekstraomgangar og straffesparkkonkurranse tel ikkje for H/U/B.

---

## Sidebets og kort

Kort-sidebet brukar desse reglane:

- gult kort = 1 kortpoeng
- direkte raudt kort = 2 kortpoeng
- to gule og dermed raudt = 2 kortpoeng totalt

Det raude kortet som kjem automatisk etter to gule tel ikkje ekstra.

---

## Konfigurasjon

Aktiv turnering er sett i `HomeController.cs`:

```csharp
private const string TournamentId = "56";
```

Aktuelle verdiar:

- `56` = World Cup 2026
- `59` = Euro 2024

Sidebet-fristen er sett til 2 timar før første kamp:

```csharp
private static readonly DateTime SideBetDeadline = new(2026, 6, 11, 19, 0, 0);
```

Første kamp startar 11. juni 2026 kl. 21:00 norsk tid.

---

## Database

Prosjektet brukar Entity Framework Core og SQL Server.

Viktige tabellar:

- `AspNetUsers`
- `CompetitionGroups`
- `Matches`
- `MatchBettings`
- `SideBettings`
- `FootballPlayers`
- `Logs`

---

## Køyr lokalt

### 1. Klon repoet

```bash
git clone https://github.com/Vigdals/MatchBetting
```

### 2. Opprett `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MatchBetting;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Køyr migreringar

I Package Manager Console:

```powershell
Update-Database
```

Ved modellendringar:

```powershell
Add-Migration <Namn>
Update-Database
```

### 4. Start appen

Start prosjektet med IIS Express frå Visual Studio.

Lokalt køyrer appen normalt på:

```text
https://localhost:44303
```

---

## Produksjon

I produksjon blir connection string henta frå Azure Key Vault.

Appen brukar:

- Azure App Service
- Azure SQL
- Azure Key Vault
- ASP.NET Core Identity
- Entity Framework Core

---

## Lisens

MIT

---

## Kontakt

GitHub: **@Vigdals**
