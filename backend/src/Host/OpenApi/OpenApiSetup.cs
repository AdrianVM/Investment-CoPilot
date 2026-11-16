using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentCoPilot.Host.OpenApi;

internal static class OpenApiSetup
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddApiDocument(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Investment Co-Pilot API",
                    Version = "v1",
                };

                // Every operation requires an OIDC access token sent as a bearer token.
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Access token from the configured OpenID Connect provider.",
                };
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
                });
                return Task.CompletedTask;
            });
        });
}
