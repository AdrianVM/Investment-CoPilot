using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace InvestmentCoPilot.Host.Authentication;

internal static class AuthenticationSetup
{
    /// <summary>
    /// Validates standard JWT access tokens from the configured OpenID Connect provider and requires an
    /// authenticated user on every endpoint unless the endpoint explicitly allows anonymous access.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services, IConfiguration configuration, bool validateOnStart = true)
    {
        var auth = services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations();
        if (validateOnStart)
        {
            auth.ValidateOnStart();
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((jwt, auth) =>
            {
                jwt.Authority = auth.Value.Authority;
                jwt.Audience = auth.Value.Audience;
                jwt.RequireHttpsMetadata = auth.Value.RequireHttpsMetadata;

                // Keep standard claim names (sub, email, name) instead of mapping them to long WS-* URIs.
                jwt.MapInboundClaims = false;

                jwt.TokenValidationParameters.ValidateIssuer = true;
                jwt.TokenValidationParameters.ValidateAudience = true;
                jwt.TokenValidationParameters.ValidateLifetime = true;
                jwt.TokenValidationParameters.ValidateIssuerSigningKey = true;

                // A token is rejected as soon as its expiry time has passed (the default allows 5 minutes).
                jwt.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
