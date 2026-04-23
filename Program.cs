using AuraLabsLicenseApi.Models;
using AuraLabsLicenseApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminTools", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddHttpClient<SupabaseRestClient>();
builder.Services.AddSingleton<LicenseKeyService>();
builder.Services.AddSingleton<AdminAuthService>();
builder.Services.AddScoped<LicenseRepository>();
builder.Services.AddScoped<LicenseWorkflowService>();

var app = builder.Build();

app.UseCors("AdminTools");

app.MapGet("/", () => Results.Ok(new
{
    name = "Aura Labs License API",
    status = "online",
    timeUtc = DateTime.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timeUtc = DateTime.UtcNow
}));

app.MapPost("/api/license/activate", async (
    LicenseActivationRequest request,
    LicenseWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    var result = await workflow.ActivateAsync(request, cancellationToken);
    return result.Valid ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/admin/login", (
    AdminLoginRequest request,
    AdminAuthService auth) =>
{
    return auth.IsValid(request.Password)
        ? Results.Ok(new { ok = true, message = "Admin unlocked." })
        : Results.Unauthorized();
});

app.MapGet("/api/admin/licenses", async (
    HttpRequest request,
    AdminAuthService auth,
    LicenseRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(request))
    {
        return Results.Unauthorized();
    }

    var records = await repository.GetLicensesAsync(cancellationToken);
    return Results.Ok(records);
});

app.MapPost("/api/admin/licenses/generate", async (
    HttpRequest httpRequest,
    GenerateLicensesRequest request,
    AdminAuthService auth,
    LicenseWorkflowService workflow,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(httpRequest))
    {
        return Results.Unauthorized();
    }

    var generated = await workflow.GenerateLicensesAsync(request, cancellationToken);
    return Results.Ok(generated);
});

app.MapPost("/api/admin/licenses/{licenseId:guid}/revoke", async (
    Guid licenseId,
    HttpRequest request,
    AdminAuthService auth,
    LicenseRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(request))
    {
        return Results.Unauthorized();
    }

    await repository.SetRevokedAsync(licenseId, true, cancellationToken);
    await repository.AddAuditLogAsync(licenseId, "revoked", "License canceled by admin.", cancellationToken);
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/admin/licenses/{licenseId:guid}/restore", async (
    Guid licenseId,
    HttpRequest request,
    AdminAuthService auth,
    LicenseRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(request))
    {
        return Results.Unauthorized();
    }

    await repository.SetRevokedAsync(licenseId, false, cancellationToken);
    await repository.AddAuditLogAsync(licenseId, "restored", "License restored by admin.", cancellationToken);
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/admin/licenses/{licenseId:guid}/extend", async (
    Guid licenseId,
    ExtendLicenseRequest request,
    HttpRequest httpRequest,
    AdminAuthService auth,
    LicenseRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(httpRequest))
    {
        return Results.Unauthorized();
    }

    if (request.Days < 1 || request.Days > 3650)
    {
        return Results.BadRequest(new { ok = false, message = "Days must be between 1 and 3650." });
    }

    var license = await repository.GetLicenseByIdAsync(licenseId, cancellationToken);
    if (license is null)
    {
        return Results.NotFound(new { ok = false, message = "License not found." });
    }

    if (license.ExpiresAt is null)
    {
        return Results.BadRequest(new { ok = false, message = "Lifetime licenses do not need more time." });
    }

    var baseDate = license.ExpiresAt < DateTimeOffset.UtcNow
        ? DateTimeOffset.UtcNow
        : license.ExpiresAt.Value;

    await repository.UpdateExpiryAsync(licenseId, baseDate.AddDays(request.Days), cancellationToken);
    await repository.AddAuditLogAsync(licenseId, "extended", $"Added {request.Days} day(s).", cancellationToken);
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/admin/licenses/{licenseId:guid}/reset-hwid", async (
    Guid licenseId,
    HttpRequest request,
    AdminAuthService auth,
    LicenseRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!auth.IsAuthorized(request))
    {
        return Results.Unauthorized();
    }

    await repository.DeleteActivationsAsync(licenseId, cancellationToken);
    await repository.AddAuditLogAsync(licenseId, "reset_hwid", "All HWID bindings removed by admin.", cancellationToken);
    return Results.Ok(new { ok = true });
});

app.Run();
