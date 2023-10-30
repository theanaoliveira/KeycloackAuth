using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Sdk.Admin;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var authenticationOptions = builder.Configuration.GetSection(KeycloakAuthenticationOptions.Section).Get<KeycloakAuthenticationOptions>();
var authorizationOptions = builder.Configuration.GetSection(KeycloakProtectionClientOptions.Section).Get<KeycloakProtectionClientOptions>();

builder.Services.AddKeycloakAuthentication(authenticationOptions);
builder.Services.AddKeycloakAuthorization(authorizationOptions);

var adminClientOptions = builder.Configuration.GetSection(KeycloakAdminClientOptions.Section).Get<KeycloakAdminClientOptions>();

builder.Services.AddKeycloakAdminHttpClient(adminClientOptions);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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