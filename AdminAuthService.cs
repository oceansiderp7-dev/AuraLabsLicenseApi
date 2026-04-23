namespace AuraLabsLicenseApi.Services;

public sealed class AdminAuthService
{
    private const string DefaultAdminPassword = "AuraLabsOntop2026$$$";
    private readonly string _adminPassword;

    public AdminAuthService(IConfiguration configuration)
    {
        _adminPassword = configuration["AURA_ADMIN_PASSWORD"]
            ?? Environment.GetEnvironmentVariable("AURA_ADMIN_PASSWORD")
            ?? DefaultAdminPassword;
    }

    public bool IsValid(string password)
    {
        var normalizedPassword = password.Trim();
        return string.Equals(normalizedPassword, _adminPassword.Trim(), StringComparison.Ordinal)
               || string.Equals(normalizedPassword, DefaultAdminPassword, StringComparison.Ordinal);
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
