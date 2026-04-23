using System.Globalization;
using System.Net;
using AuraLabsLicenseApi.Models;

namespace AuraLabsLicenseApi.Services;

public sealed class LicenseRepository
{
    private readonly SupabaseRestClient _client;

    public LicenseRepository(SupabaseRestClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<SupabaseLicenseRecord>> GetLicensesAsync(CancellationToken cancellationToken)
    {
        return await _client.GetAsync<List<SupabaseLicenseRecord>>(
            "licenses?select=*,license_activations(*)&order=created_at.desc",
            cancellationToken) ?? new List<SupabaseLicenseRecord>();
    }

    public async Task<SupabaseLicenseRecord?> GetLicenseByIdAsync(Guid licenseId, CancellationToken cancellationToken)
    {
        var records = await _client.GetAsync<List<SupabaseLicenseRecord>>(
            $"licenses?id=eq.{licenseId}&select=*,license_activations(*)",
            cancellationToken);

        return records?.FirstOrDefault();
    }

    public async Task<SupabaseLicenseRecord?> GetLicenseByKeyHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        var records = await _client.GetAsync<List<SupabaseLicenseRecord>>(
            $"licenses?license_key_hash=eq.{Uri.EscapeDataString(keyHash)}&select=*,license_activations(*)",
            cancellationToken);

        return records?.FirstOrDefault();
    }

    public async Task CreateLicenseAsync(GeneratedLicenseResponse generated, string keyHash, CancellationToken cancellationToken)
    {
        var body = new
        {
            license_key_hash = keyHash,
            license_type = generated.LicenseType,
            expires_at = generated.ExpiresAt,
            is_revoked = false,
            max_devices = generated.MaxDevices
        };

        await _client.PostAsync("licenses", body, cancellationToken);
    }

    public async Task AddActivationAsync(Guid licenseId, string hwidHash, string hwidDisplay, CancellationToken cancellationToken)
    {
        var body = new
        {
            license_id = licenseId,
            hwid_hash = hwidHash,
            hwid_display = hwidDisplay,
            last_seen_at = DateTimeOffset.UtcNow
        };

        await _client.PostAsync("license_activations", body, cancellationToken);
    }

    public async Task TouchActivationAsync(Guid activationId, CancellationToken cancellationToken)
    {
        await _client.PatchAsync(
            $"license_activations?id=eq.{activationId}",
            new { last_seen_at = DateTimeOffset.UtcNow },
            cancellationToken);
    }

    public async Task SetRevokedAsync(Guid licenseId, bool revoked, CancellationToken cancellationToken)
    {
        await _client.PatchAsync(
            $"licenses?id=eq.{licenseId}",
            new { is_revoked = revoked, updated_at = DateTimeOffset.UtcNow },
            cancellationToken);
    }

    public async Task UpdateExpiryAsync(Guid licenseId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await _client.PatchAsync(
            $"licenses?id=eq.{licenseId}",
            new { expires_at = expiresAt, updated_at = DateTimeOffset.UtcNow },
            cancellationToken);
    }

    public async Task DeleteActivationsAsync(Guid licenseId, CancellationToken cancellationToken)
    {
        await _client.DeleteAsync($"license_activations?license_id=eq.{licenseId}", cancellationToken);
    }

    public async Task AddAuditLogAsync(Guid? licenseId, string action, string details, CancellationToken cancellationToken)
    {
        var body = new
        {
            license_id = licenseId,
            action,
            details
        };

        await _client.PostAsync("license_audit_logs", body, cancellationToken);
    }
}
