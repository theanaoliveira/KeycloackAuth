using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.OpenApi.Models;
using OpenIddict.Client;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using static OpenIddict.Abstractions.OpenIddictConstants;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Log.Logger);

var server = Environment.GetEnvironmentVariable("KEYCLOAK_SERVER_URL");
var realm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM");
var issuer = $"{server}/realms/{realm}";
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    
}).AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
    options.SlidingExpiration = false;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note: the validation handler uses OpenID Connect discovery
        // to retrieve the issuer signing keys used to validate tokens.
        options.SetIssuer(issuer);

        // Configure the validation handler to use introspection and register the client
        // credentials used when communicating with the remote introspection endpoint.
        //options.SetClientId(clientId);
        //options.SetClientSecret(clientSecret);
        options.UseIntrospection().SetClientId(clientId).SetClientSecret(clientSecret);

        options.Configure(options =>
        {
            options.TokenValidationParameters.ValidIssuers = new List<string> { $"{server}/realms/{realm}" };
        });

        // Register the System.Net.Http integration.
        options.UseSystemNetHttp();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    }).AddClient(options =>
    {

        // Note: this sample uses the authorization code and refresh token
        // flows, but you can enable the other flows if necessary.
        options.AllowAuthorizationCodeFlow()
                .AllowRefreshTokenFlow();

        // Register the signing and encryption credentials used to protect
        // sensitive data like the state tokens produced by OpenIddict.
        options.AddEphemeralEncryptionKey()
                .AddEphemeralSigningKey();

        // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
        options.UseAspNetCore()
                .EnableStatusCodePagesIntegration()
                .EnableRedirectionEndpointPassthrough()
                .DisableTransportSecurityRequirement()
                .EnablePostLogoutRedirectionEndpointPassthrough();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
                .SetProductInformation(typeof(Program).Assembly);


        options.DisableTokenStorage();
        // Add a client registration matching the client application definition in the server project.

        options.AddRegistration(new OpenIddictClientRegistration
        {
            RegistrationId = "keycloak",
            Issuer = new Uri(issuer, UriKind.Absolute),
            ProviderName = "keycloak",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = { Scopes.Email, Scopes.Profile, Scopes.OpenId },

            RedirectUri = new Uri("callback/login/keycloak", UriKind.Relative),
            PostLogoutRedirectUri = new Uri("callback/logout/keycloak", UriKind.Relative),
        });
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Test", Version = "v1" });

    // Configura��o para autentica��o OAuth2 com Keycloak
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{server}/realms/{realm}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{server}/realms/{realm}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "email", "email" },
                    { "roles", "Access the API" },
                    { "profile", "profile" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new string[] { }
        }
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(s =>
    {
        s.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseRewriter(Rewrite());

app.Run();

RewriteOptions Rewrite()
{
    var option = new RewriteOptions();
    option.AddRedirect("^$", "swagger");

    return option;
}