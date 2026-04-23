using AuraLabsLicenseApi.Models;

namespace AuraLabsLicenseApi.Services;

public sealed class LicenseWorkflowService
{
    private readonly LicenseKeyService _licenseKeyService;
    private readonly LicenseRepository _repository;

    public LicenseWorkflowService(LicenseKeyService licenseKeyService, LicenseRepository repository)
    {
        _licenseKeyService = licenseKeyService;
        _repository = repository;
    }

    public async Task<LicenseActivationResponse> ActivateAsync(LicenseActivationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey))
        {
            return Invalid("Enter a license key.");
        }

        if (string.IsNullOrWhiteSpace(request.HwidHash))
        {
            return Invalid("Missing hardware ID.");
        }

        var keyHash = LicenseKeyService.HashKey(request.LicenseKey);
        var license = await _repository.GetLicenseByKeyHashAsync(keyHash, cancellationToken);
        if (license is null)
        {
            return Invalid("License key was not found online.");
        }

        if (license.IsRevoked)
        {
            return Invalid("This license key has been canceled.", license);
        }

        if (license.ExpiresAt is not null && license.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Invalid("This license key has expired.", license);
        }

        var activations = license.Activations;
        var existingActivation = activations.FirstOrDefault(item =>
            string.Equals(item.HwidHash, request.HwidHash, StringComparison.OrdinalIgnoreCase));

        if (existingActivation is not null)
        {
            await _repository.TouchActivationAsync(existingActivation.Id, cancellationToken);
            return Valid("License accepted.", license, activations.Count);
        }

        if (activations.Count >= license.MaxDevices)
        {
            return Invalid("This key is already assigned to another user or machine.", license);
        }

        await _repository.AddActivationAsync(license.Id, request.HwidHash, request.HwidDisplay, cancellationToken);
        await _repository.AddAuditLogAsync(license.Id, "activated", $"Activated by {request.HwidDisplay}. App version: {request.AppVersion}", cancellationToken);
        return Valid("License accepted and bound to this device.", license, activations.Count + 1);
    }

    public async Task<IReadOnlyList<GeneratedLicenseResponse>> GenerateLicensesAsync(GenerateLicensesRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 1 || request.Quantity > 50)
        {
            throw new InvalidOperationException("Generate between 1 and 50 keys at a time.");
        }

        var results = new List<GeneratedLicenseResponse>();
        for (var index = 0; index < request.Quantity; index++)
        {
            var generated = _licenseKeyService.Generate(request.Duration, request.MaxDevices);
            await _repository.CreateLicenseAsync(generated, LicenseKeyService.HashKey(generated.LicenseKey), cancellationToken);
            await _repository.AddAuditLogAsync(null, "generated", $"Generated {generated.LicenseType} license.", cancellationToken);
            results.Add(generated);
        }

        return results;
    }

    private static LicenseActivationResponse Valid(string message, SupabaseLicenseRecord license, int usedDevices)
    {
        return new LicenseActivationResponse
        {
            Valid = true,
            Message = message,
            LicenseType = license.LicenseType,
            ExpiresAt = license.ExpiresAt,
            IsRevoked = license.IsRevoked,
            MaxDevices = license.MaxDevices,
            UsedDevices = usedDevices
        };
    }

    private static LicenseActivationResponse Invalid(string message, SupabaseLicenseRecord? license = null)
    {
        return new LicenseActivationResponse
        {
            Valid = false,
            Message = message,
            LicenseType = license?.LicenseType ?? string.Empty,
            ExpiresAt = license?.ExpiresAt,
            IsRevoked = license?.IsRevoked ?? false,
            MaxDevices = license?.MaxDevices ?? 1,
            UsedDevices = license?.Activations.Count ?? 0
        };
    }
}
