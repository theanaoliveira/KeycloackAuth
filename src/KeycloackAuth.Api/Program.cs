using Microsoft.AspNetCore.Rewrite;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
            .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

var server = Environment.GetEnvironmentVariable("KEYCLOAK_SERVER_URL");
var realm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM");
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Note: the validation handler uses OpenID Connect discovery
        // to retrieve the issuer signing keys used to validate tokens.
        options.SetIssuer($"{server}/realms/{realm}");

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
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Test", Version = "v1" });

    // Configuração para autenticação OAuth2 com Keycloak
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
                    { "oauth-api-client-scope", "Access the API" },
                    { "profile", "profile" }
                    // Adicione outros scopes conforme necessário
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
        s.OAuthClientId(clientId);
        s.OAuthClientSecret(clientSecret);
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