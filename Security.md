# Security

This document describes the security design and operational practices for the **MatchBetting** MVC application.

The goals are:
- Protect database credentials and user data.
- Keep secrets out of source control.
- Support a clean separation between development and production environments.

---

## 1. Architecture overview

- ASP.NET Core MVC application with Identity (user accounts, login).
- Uses **Entity Framework Core** with a SQL Server backend.
- Hosted on **Azure App Service** in production.
- Data stored in **Azure SQL Database** (MatchBetting database).
- No external login providers at the moment.

---

## 2. Environments

### Development

- Runs locally on the developer machine.
- Uses `ConnectionStrings:DefaultConnection` (or `DatabaseConnection`) from `appsettings.json` pointing to:
  - LocalDB / local SQL instance.
- Database schema is managed via EF Core migrations.
- Migrations are applied locally with:
  - `Update-Database` (Package Manager Console) or
  - `dotnet ef database update`.

### Production

- Runs on Azure App Service.
- Uses **Azure Key Vault** for the production database connection string.
- EF Core migrations must be applied against the Azure SQL database before or during deployment.

---

## 3. Secrets and configuration

### 3.1 Data flow

- `appsettings.json` contains:
  - Local development connection string (`DefaultConnection` or equivalent).
  - `KeyVault:Url` (Key Vault endpoint, not a secret).
- On startup, `Program.cs`:

  1. Reads `KeyVault:Url` from configuration.
  2. Adds the Azure Key Vault configuration provider with `DefaultAzureCredential`.
  3. Selects connection string based on environment:

     ```csharp
     if (builder.Environment.IsDevelopment())
     {
         connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
     }
     else
     {
         connectionString = builder.Configuration["db-connection-matchBetting"];
     }
     ```

- In production, the database connection string is stored as a Key Vault secret:

  - `db-connection-matchBetting`

- The App Service uses Managed Identity (or a service principal) with `Key Vault Secrets User` (or similar) on the vault.

### 3.2 Guidelines

- No secrets (DB passwords, API keys, etc.) are allowed in:
  - `Program.cs`
  - `appsettings*.json` checked into Git
  - CI/CD pipelines in plain text.
- All long-lived production secrets must be stored in **Azure Key Vault**.
- For development, secrets can be stored using:
  - `dotnet user-secrets`
  - local environment variables
  - localdb connection strings in `appsettings.Development.json` that are safe and not reused in production.

---

## 4. Database security

### Production (Azure SQL – MatchBetting database)

- Connection string is only stored in Key Vault.
- Application access should use:
  - Managed Identity if feasible, or a dedicated SQL user with minimal privileges.
- Enable:
  - Transparent Data Encryption (TDE).
  - SQL auditing / threat detection for monitoring.
- Firewall:
  - Restrict inbound access to App Service (via service tags/VNet/private endpoint).
  - Allow selected admin IPs for management.

### Development

- LocalDB / dev SQL instance is used only for development.
- Do not copy raw production data into development.
- If production data is required for debugging, anonymise or mask it first.

---

## 5. Authentication and authorization

- Uses ASP.NET Core Identity (`IdentityUser`) with local accounts.
- `RequireConfirmedAccount` is `false`:
  - Acceptable for private/demo usage, but consider turning it on if exposed publicly.
- Role management can be added for admin pages:
  - Use `[Authorize(Roles = "Admin")]` for any management UI.
- Password policies and lockout settings should be configured via Identity options if the app becomes public-facing.

---

## 6. Logging and monitoring

- Uses ASP.NET Core logging with configuration from `Logging` section.
- Requirements:
  - Do not log passwords, tokens or full connection strings.
  - Avoid logging personal data beyond what is strictly necessary for debugging.
- Production recommendation:
  - Use Application Insights or similar for structured logs and request tracing.
  - Apply sensible retention and role-based access controls to log storage.

---

## 7. Dependencies and supply chain

- All dependencies are NuGet packages managed via the SDK.
- Recommended practices:
  - Keep .NET and packages updated.
  - Enable Dependabot and GitHub security alerts on the repository.
  - Avoid untrusted or unmaintained packages.
  - If npm / frontend tooling is added in the future, apply the same controls:
    - lockfiles, trusted registries, and vulnerability scanning.

---

## 8. Deployment and migrations

- Deployment target: Azure App Service (Windows, .NET 8).
- App uses EF Core migrations. Any changes to the model must be accompanied by migrations.

### Migration process (production)

1. Generate migration locally:

   ```powershell
   Add-Migration <Name>
