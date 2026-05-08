using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using WebUtilities.Application;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? throw new InvalidOperationException("Authentication:Google:ClientId is not configured.");
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is not configured.");
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"]
    ?? throw new InvalidOperationException("Authentication:GitHub:ClientId is not configured.");
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]
    ?? throw new InvalidOperationException("Authentication:GitHub:ClientSecret is not configured.");

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});

authBuilder.AddIdentityCookies();
authBuilder.AddBearerToken(IdentityConstants.BearerScheme);
authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    })
    .AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
    });

builder.Services.AddAuthorization();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<ApplicationUser>();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
