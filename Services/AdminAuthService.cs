namespace AuraLabsLicenseApi.Services;

public sealed class AdminAuthService
{
    private readonly string _adminPassword;

    public AdminAuthService(IConfiguration configuration)
    {
        _adminPassword = configuration["AURA_ADMIN_PASSWORD"]
            ?? Environment.GetEnvironmentVariable("AURA_ADMIN_PASSWORD")
            ?? "AuraLabsOntop2026$$$";
    }

    public bool IsValid(string password)
    {
        return string.Equals(password, _adminPassword, StringComparison.Ordinal);
    }

    public bool IsAuthorized(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("X-Admin-Password", out var password))
        {
            return false;
        }

        return IsValid(password.ToString());
    }
}
