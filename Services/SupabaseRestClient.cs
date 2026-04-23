using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AuraLabsLicenseApi.Services;

public sealed class SupabaseRestClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SupabaseRestClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = (configuration["SUPABASE_URL"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? string.Empty).TrimEnd('/');
        _serviceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"] ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_serviceRoleKey))
        {
            throw new InvalidOperationException("Set SUPABASE_URL and SUPABASE_SERVICE_ROLE_KEY before starting the API.");
        }
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    public async Task PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Post, path, body);
        request.Headers.Add("Prefer", "return=minimal");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task PatchAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Patch, path, body);
        request.Headers.Add("Prefer", "return=minimal");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Delete, path);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_supabaseUrl}/rest/v1/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        request.Headers.Add("apikey", _serviceRoleKey);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Supabase request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }
}
