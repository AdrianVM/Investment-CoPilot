using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentCoPilot.Host.OpenApi;

internal static class OpenApiSetup
{
    private const string BearerScheme = "Bearer";

    private static readonly HashSet<Type> NumericTypes =
    [
        typeof(decimal), typeof(double), typeof(float),
        typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
    ];

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

            // The API writes numbers (money, ratios, counts) as JSON numbers. ASP.NET Core documents them as "number or
            // string" because it also accepts strings on input, which would type them as number | string in the
            // generated client. Document what the API sends: a number.
            options.AddSchemaTransformer((schema, context, _) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (NumericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type) && schema.Type is { } schemaType)
                {
                    schema.Type = schemaType & ~JsonSchemaType.String;
                    schema.Pattern = null;
                }

                return Task.CompletedTask;
            });
        });
}
