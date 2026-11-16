using System.Net.Http.Headers;
using InvestmentCoPilot.SharedKernel.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace InvestmentCoPilot.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against the test Postgres container, trusting tokens from <see cref="TestIssuer"/>.
/// </summary>
public class ApiFactory(string connectionString, string environment = "Development") : WebApplicationFactory<Program>
{
    private readonly List<Action<IServiceCollection>> testServices = [];
    private readonly Dictionary<string, string> settings = [];

    public ApiFactory(PostgresFixture postgres)
        : this(postgres.ConnectionString)
    {
    }

    /// <summary>Adds test-only endpoints, mapped under <c>/api</c> like any module's endpoints.</summary>
    public ApiFactory WithEndpoints<TEndpoints>()
        where TEndpoints : class, IEndpointModule
    {
        testServices.Add(services => services.AddSingleton<IEndpointModule, TEndpoints>());
        return this;
    }

    /// <summary>Adds or replaces services in the API before it starts.</summary>
    public ApiFactory WithServices(Action<IServiceCollection> configure)
    {
        testServices.Add(configure);
        return this;
    }

    /// <summary>Overrides a configuration setting, for example <c>Auth:Claims:Email</c>.</summary>
    public ApiFactory WithSetting(string key, string value)
    {
        settings[key] = value;
        return this;
    }

    /// <summary>A client that sends a valid access token for <paramref name="user"/>.</summary>
    public HttpClient CreateClientAs(TestUser user) => CreateClientWithToken(TestIssuer.CreateToken(user));

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Database", connectionString);
        builder.UseSetting("Auth:Authority", TestIssuer.Issuer);
        builder.UseSetting("Auth:Audience", TestIssuer.Audience);
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            // Supply the issuer's metadata directly instead of downloading its discovery document.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = TestIssuer.Issuer };
                configuration.SigningKeys.Add(TestIssuer.SigningKey);
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

            foreach (var configure in testServices)
            {
                configure(services);
            }
        });
    }
}
