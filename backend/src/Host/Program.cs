using InvestmentCoPilot.Host.Authentication;
using InvestmentCoPilot.Host.Health;
using InvestmentCoPilot.Host.OpenApi;
using InvestmentCoPilot.Assessment;
using InvestmentCoPilot.Identity;
using InvestmentCoPilot.MarketData;
using InvestmentCoPilot.SharedKernel.Persistence;
using InvestmentCoPilot.SharedKernel.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityModule()
    .AddMarketDataModule()
    .AddAssessmentModule();

builder.Services.AddApiAuthentication(builder.Configuration, validateOnStart: !DocumentGeneration.IsRunning);

if (!DocumentGeneration.IsRunning)
{
    // Fail at startup, not on the first request, when the database is not configured.
    builder.Services.AddOptions<DatabaseSettings>()
        .Validate(
            _ => !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(ModuleDbContextRegistration.ConnectionStringName)),
            $"Connection string '{ModuleDbContextRegistration.ConnectionStringName}' is not configured.")
        .ValidateOnStart();
}
builder.Services.AddApiHealthChecks();
builder.Services.AddApiDocument();

var app = builder.Build();

if (app.Environment.IsDevelopment() && !DocumentGeneration.IsRunning)
{
    // Only in Development. Other environments apply migrations as an explicit step (dotnet ef database update),
    // because migrating at startup is unsafe when several instances start at once.
    await app.Services.MigrateModuleDatabasesAsync();
}

app.UseAuthentication();
app.UseCurrentUser();
app.UseAuthorization();

app.MapApiHealthChecks();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous(); // /openapi/v1.json
}

var api = app.MapGroup("/api");
foreach (var endpointModule in app.Services.GetServices<IEndpointModule>())
{
    endpointModule.MapEndpoints(api);
}

await app.RunAsync();

/// <summary>Marker for validating database settings at startup.</summary>
internal sealed class DatabaseSettings;

/// <summary>Entry point, public so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
