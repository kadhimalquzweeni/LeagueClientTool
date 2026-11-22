using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Executes user-authored requests against the local League Client API.
/// Only common read/write HTTP methods and validated local API paths are accepted.
/// </summary>
public sealed class LeagueCustomRequestService(
    ILeagueClientDetector detector,
    ILeagueClientApiClientFactory apiClientFactory) : ILeagueCustomRequestService
{
    private static readonly IReadOnlyDictionary<string, HttpMethod> AllowedMethods =
        new Dictionary<string, HttpMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["GET"] = HttpMethod.Get,
            ["POST"] = HttpMethod.Post,
            ["PUT"] = HttpMethod.Put,
            ["PATCH"] = HttpMethod.Patch,
            ["DELETE"] = HttpMethod.Delete
        };

    public async Task<LeagueClientOperationResult> SendAsync(
        string method,
        string endpoint,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        if (!AllowedMethods.TryGetValue(method.Trim(), out HttpMethod? httpMethod))
        {
            throw new ArgumentException("Only GET, POST, PUT, PATCH and DELETE are supported.", nameof(method));
        }

        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null)
        {
            return new LeagueClientOperationResult(
                false,
                "League Client is not running, or the lockfile could not be read.");
        }

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        LeagueClientApiResponse response = await api.SendAsync(
            httpMethod,
            endpoint,
            body,
            cancellationToken);

        string message = response.IsSuccessStatusCode
            ? $"Request completed with HTTP {response.StatusCode}."
            : $"League Client returned HTTP {response.StatusCode}.";

        return new LeagueClientOperationResult(
            response.IsSuccessStatusCode,
            message,
            response.StatusCode,
            response.Body);
    }
}
