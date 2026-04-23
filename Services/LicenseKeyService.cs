using System.Security.Cryptography;
using System.Text;
using AuraLabsLicenseApi.Helpers;
using AuraLabsLicenseApi.Models;

namespace AuraLabsLicenseApi.Services;

public sealed class LicenseKeyService
{
    private const string ProductPrefix = "ALP1";
    private static readonly DateTimeOffset LicenseEpochUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly string _signingSecret;

    public LicenseKeyService(IConfiguration configuration)
    {
        _signingSecret = configuration["AURA_LICENSE_SIGNING_SECRET"]
            ?? Environment.GetEnvironmentVariable("AURA_LICENSE_SIGNING_SECRET")
            ?? "AuraLabsPacker::OfflineLicense::v2::R8T3N1Y6U4W2P9K5";
    }

    public GeneratedLicenseResponse Generate(string duration, int maxDevices)
    {
        var normalizedDuration = duration.Trim().ToLowerInvariant();
        var licenseType = normalizedDuration switch
        {
            "2weeks" or "2-weeks" or "two-weeks" => "2 Weeks",
            "1month" or "1-month" or "month" => "1 Month",
            "lifetime" => "Lifetime",
            _ => throw new InvalidOperationException("Duration must be 2weeks, 1month, or lifetime.")
        };

        var expiresAt = licenseType switch
        {
            "2 Weeks" => DateTimeOffset.UtcNow.Date.AddDays(14),
            "1 Month" => DateTimeOffset.UtcNow.Date.AddDays(30),
            _ => (DateTimeOffset?)null
        };

        var payloadBytes = BuildPayloadBytes(licenseType, expiresAt);
        var payload = Base32Encoder.Encode(payloadBytes).Substring(0, 15);
        var signature = ComputeSignature(payload);

        return new GeneratedLicenseResponse
        {
            LicenseKey = FormatKey(payload, signature),
            LicenseType = licenseType,
            ExpiresAt = expiresAt,
            MaxDevices = Math.Clamp(maxDevices, 1, 10)
        };
    }

    public static string NormalizeKey(string keyInput)
    {
        return new string(keyInput
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    public static string HashKey(string keyInput)
    {
        var normalized = NormalizeKey(keyInput);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }

    private byte[] BuildPayloadBytes(string licenseType, DateTimeOffset? expiresAt)
    {
        using var rng = RandomNumberGenerator.Create();
        var payloadBytes = new byte[9];
        var tierCode = licenseType switch
        {
            "2 Weeks" => 1,
            "1 Month" => 2,
            "Lifetime" => 15,
            _ => throw new InvalidOperationException("Unsupported license type.")
        };

        payloadBytes[0] = (byte)((1 << 4) | tierCode);

        var expiryValue = expiresAt is null
            ? uint.MaxValue
            : (uint)(expiresAt.Value.Date - LicenseEpochUtc).TotalDays;

        payloadBytes[1] = (byte)(expiryValue >> 24);
        payloadBytes[2] = (byte)(expiryValue >> 16);
        payloadBytes[3] = (byte)(expiryValue >> 8);
        payloadBytes[4] = (byte)expiryValue;

        var randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        Buffer.BlockCopy(randomBytes, 0, payloadBytes, 5, randomBytes.Length);
        return payloadBytes;
    }

    private string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ProductPrefix}:{payload}"));
        return Base32Encoder.Encode(hash).Substring(0, 10);
    }

    private static string FormatKey(string payload, string signature)
    {
        return $"{ProductPrefix}-{payload[..5]}-{payload.Substring(5, 5)}-{payload.Substring(10, 5)}-{signature[..5]}-{signature.Substring(5, 5)}";
    }
}
