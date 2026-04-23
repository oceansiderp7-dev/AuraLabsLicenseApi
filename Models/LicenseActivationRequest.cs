namespace AuraLabsLicenseApi.Models;

public sealed class LicenseActivationRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HwidHash { get; set; } = string.Empty;
    public string HwidDisplay { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
}
