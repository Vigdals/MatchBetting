using Azure.Identity;
using MatchBetting.Data;
using MatchBetting.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrlString = builder.Configuration["KeyVault:Url"]
                            ?? throw new InvalidOperationException("KeyVault:Url is not configured.");
    var keyVaultUrl = new Uri(keyVaultUrlString);

    builder.Configuration.AddAzureKeyVault(keyVaultUrl, new DefaultAzureCredential());

}

string connectionString;
if (builder.Environment.IsDevelopment())
{
    // Lokal DB i utvikling
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("DefaultConnection not found in appsettings.");
}
else
{
    // Prod – hent frå Key Vault
    connectionString = builder.Configuration["db-connection-matchBetting"]
                       ?? throw new InvalidOperationException("Key Vault secret 'db-connection-matchBetting' not found.");
}


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Register services
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<INifsApiService, NifsApiService>();

var app = builder.Build();

// pipeline som før ...
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{matchId?}");
});

app.MapRazorPages();

app.Run();