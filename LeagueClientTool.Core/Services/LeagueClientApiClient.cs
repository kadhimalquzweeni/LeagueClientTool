using LeagueClientTool.Core.Interfaces;

namespace LeagueClientTool.Core.Services;

public sealed class LeagueClientApiClient(HttpClient httpClient) : ILeagueClientApiClient
{
    public async Task<string> GetAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        if (endpoint.StartsWith("//", StringComparison.Ordinal) ||
            endpoint.StartsWith(@"\\", StringComparison.Ordinal) ||
            !Uri.TryCreate(endpoint, UriKind.Relative, out _))
        {
            throw new ArgumentException(
                "The endpoint must be a relative League Client API path.",
                nameof(endpoint));
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose() => httpClient.Dispose();
}
