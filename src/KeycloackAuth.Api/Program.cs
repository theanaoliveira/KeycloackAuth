using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var server = Environment.GetEnvironmentVariable("KEYCLOAK_SERVER_URL");
var realm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM");
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = $"{server}/realms/{realm}";
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.RequireHttpsMetadata = false;
        options.MetadataAddress = $"{server}/realms/{realm}/.well-known/openid-configuration";
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
                    { "openid", "OpenID scope" },
                    { "profile", "Profile scope" },
                    // Adicione outros escopos conforme necessário
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
            Array.Empty<string>()
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
        s.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        s.OAuthClientId("oauth-api");
        s.OAuthAppName("Swagger UI");
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