namespace AuraLabsLicenseApi.Models;

public sealed class GeneratedLicenseResponse
{
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public int MaxDevices { get; set; }
}
