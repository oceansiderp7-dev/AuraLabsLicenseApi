namespace AuraLabsLicenseApi.Models;

public sealed class GenerateLicensesRequest
{
    public string Duration { get; set; } = "2weeks";
    public int Quantity { get; set; } = 1;
    public int MaxDevices { get; set; } = 1;
}
