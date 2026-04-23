using System.Text.Json.Serialization;

namespace AuraLabsLicenseApi.Models;

public sealed class SupabaseActivationRecord
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("license_id")]
    public Guid LicenseId { get; set; }

    [JsonPropertyName("hwid_hash")]
    public string HwidHash { get; set; } = string.Empty;

    [JsonPropertyName("hwid_display")]
    public string? HwidDisplay { get; set; }

    [JsonPropertyName("first_activated_at")]
    public DateTimeOffset FirstActivatedAt { get; set; }

    [JsonPropertyName("last_seen_at")]
    public DateTimeOffset LastSeenAt { get; set; }
}
