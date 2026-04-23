namespace AuraLabsLicenseApi.Models;

public sealed class LicenseActivationResponse
{
    public bool Valid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public int MaxDevices { get; set; }
    public int UsedDevices { get; set; }
}
