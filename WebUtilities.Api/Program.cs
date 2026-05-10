using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Scalar.AspNetCore;
using System.Security.Claims;
using WebUtilities.Application;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "JWT Bearer token"
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();
        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                }] = Array.Empty<string>()
            });
        }

        return Task.CompletedTask;
    });
});
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

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        options.Scope.Add("user:email");
    });
}

builder.Services.AddAuthorization();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseAuthorization();

var authGroup = app.MapGroup("/auth")
    .WithTags("Authentication");

authGroup.MapIdentityApi<ApplicationUser>();
authGroup.MapGet("/me", [Authorize] (ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;

    return Results.Ok(new
    {
        Id = userId,
        Email = email
    });
});

var allowedFrontendOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "http://localhost:5173",
    "http://localhost:3000"
};

var defaultFrontendCallback = builder.Configuration["Authentication:External:FrontendCallbackUrl"]
    ?? "http://localhost:5173/";

authGroup.MapGet("/external-login", (
    string provider,
    string? returnUrl,
    HttpContext httpContext,
    LinkGenerator linkGenerator,
    SignInManager<ApplicationUser> signInManager) =>
{
    var resolvedReturnUrl = ResolveReturnUrl(returnUrl, defaultFrontendCallback, allowedFrontendOrigins);
    if (resolvedReturnUrl is null)
    {
        return Results.BadRequest("Invalid returnUrl.");
    }

    var callbackPath = linkGenerator.GetPathByName(
        httpContext,
        endpointName: "ExternalLoginCallback",
        values: new { returnUrl = resolvedReturnUrl });

    if (string.IsNullOrWhiteSpace(callbackPath))
    {
        return Results.Problem("Failed to generate external login callback URL.");
    }

    var redirectUri = new Uri(new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}"), callbackPath)
        .ToString();
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUri);

    return Results.Challenge(properties, [provider]);
}).AllowAnonymous();

authGroup.MapGet("/external-login-callback", async (
    string? returnUrl,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var resolvedReturnUrl = ResolveReturnUrl(returnUrl, defaultFrontendCallback, allowedFrontendOrigins);
    if (resolvedReturnUrl is null)
    {
        return Results.BadRequest("Invalid returnUrl.");
    }

    var externalAuthResult = await httpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
    if (!externalAuthResult.Succeeded || externalAuthResult.Principal is null)
    {
        var failRedirect = QueryHelpers.AddQueryString(resolvedReturnUrl, "error", "external_auth_failed");
        return Results.Redirect(failRedirect);
    }

    var externalPrincipal = externalAuthResult.Principal;
    var provider = externalAuthResult.Properties?.Items["LoginProvider"];
    var providerKey = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);

    if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
    {
        var failRedirect = QueryHelpers.AddQueryString(resolvedReturnUrl, "error", "invalid_external_principal");
        return Results.Redirect(failRedirect);
    }

    var user = await userManager.FindByLoginAsync(provider, providerKey);
    if (user is null && !string.IsNullOrWhiteSpace(email))
    {
        user = await userManager.FindByEmailAsync(email);
    }

    if (user is null)
    {
        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? BuildExternalFallbackEmail(provider, providerKey)
            : email;

        user = new ApplicationUser
        {
            UserName = resolvedEmail,
            Email = resolvedEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var failRedirect = QueryHelpers.AddQueryString(resolvedReturnUrl, "error", "user_creation_failed");
            return Results.Redirect(failRedirect);
        }
    }

    var hasLogin = await userManager.GetLoginsAsync(user);
    if (!hasLogin.Any(x => x.LoginProvider == provider && x.ProviderKey == providerKey))
    {
        var addLoginResult = await userManager.AddLoginAsync(
            user,
            new UserLoginInfo(provider, providerKey, provider));

        if (!addLoginResult.Succeeded)
        {
            var failRedirect = QueryHelpers.AddQueryString(resolvedReturnUrl, "error", "external_login_link_failed");
            return Results.Redirect(failRedirect);
        }
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

    var successRedirect = QueryHelpers.AddQueryString(resolvedReturnUrl, "externalLogin", "success");
    return Results.Redirect(successRedirect);
})
.AllowAnonymous()
.WithName("ExternalLoginCallback");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static string? ResolveReturnUrl(string? requestedReturnUrl, string defaultReturnUrl, HashSet<string> allowedOrigins)
{
    var candidate = string.IsNullOrWhiteSpace(requestedReturnUrl) ? defaultReturnUrl : requestedReturnUrl;

    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
    {
        return null;
    }

    var origin = $"{uri.Scheme}://{uri.Authority}";
    return allowedOrigins.Contains(origin) ? uri.ToString() : null;
}

static string BuildExternalFallbackEmail(string provider, string providerKey)
{
    var providerPart = new string(provider.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    var keyPart = new string(providerKey.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    if (string.IsNullOrWhiteSpace(providerPart))
    {
        providerPart = "external";
    }

    if (string.IsNullOrWhiteSpace(keyPart))
    {
        keyPart = Guid.NewGuid().ToString("N");
    }

    return $"{providerPart}-{keyPart}@external.webutilities.local";
}
