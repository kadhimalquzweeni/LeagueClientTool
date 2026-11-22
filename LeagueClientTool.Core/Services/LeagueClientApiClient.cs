using System.Net;
using System.Text;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

public sealed class LeagueClientApiClient(HttpClient httpClient) : ILeagueClientApiClient
{
    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "DELETE", "PATCH"
    };

    public async Task<string> GetAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        LeagueClientApiResponse result = await SendAsync(
            HttpMethod.Get,
            endpoint,
            cancellationToken: cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"League Client API request failed with status code {result.StatusCode}: {result.Body}",
                null,
                (HttpStatusCode)result.StatusCode);
        }

        return result.Body;
    }

    public async Task<LeagueClientApiResponse> SendAsync(
        HttpMethod method,
        string endpoint,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ValidateEndpoint(endpoint);

        if (!SupportedMethods.Contains(method.Method))
        {
            throw new ArgumentException(
                $"HTTP method '{method.Method}' is not supported for League Client requests.",
                nameof(method));
        }

        using var request = new HttpRequestMessage(method, endpoint);

        if (body is not null && method != HttpMethod.Get && method != HttpMethod.Delete)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return new LeagueClientApiResponse((int)response.StatusCode, responseBody);
    }

    private static void ValidateEndpoint(string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (!endpoint.StartsWith("/", StringComparison.Ordinal) ||
            endpoint.StartsWith("//", StringComparison.Ordinal) ||
            endpoint.Contains('\\') ||
            endpoint.Any(char.IsControl) ||
            endpoint.Contains('#') ||
            !Uri.TryCreate(endpoint, UriKind.Relative, out _))
        {
            throw new ArgumentException(
                "The endpoint must be a local, absolute-path League Client API route.",
                nameof(endpoint));
        }

        string path = endpoint.Split('?', 2)[0];
        if (path.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException(
                "The endpoint cannot contain path traversal segments.",
                nameof(endpoint));
        }
    }

    public void Dispose() => httpClient.Dispose();
}
