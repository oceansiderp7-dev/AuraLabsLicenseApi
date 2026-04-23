using System.Text.Json.Serialization;

namespace AuraLabsLicenseApi.Models;

public sealed class SupabaseLicenseRecord
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("license_key_hash")]
    public string LicenseKeyHash { get; set; } = string.Empty;

    [JsonPropertyName("license_type")]
    public string LicenseType { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("is_revoked")]
    public bool IsRevoked { get; set; }

    [JsonPropertyName("max_devices")]
    public int MaxDevices { get; set; } = 1;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("license_activations")]
    public List<SupabaseActivationRecord> Activations { get; set; } = new();
}
