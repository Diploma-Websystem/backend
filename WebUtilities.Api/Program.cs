using Scalar.AspNetCore;
using WebUtilities.Application;
using WebUtilities.Core.Entities;
using WebUtilities.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddInfrastructure();

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
