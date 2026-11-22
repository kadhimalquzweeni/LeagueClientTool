namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueClientApiClient : IDisposable
{
    Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default);

    Task<LeagueClientApiResponse> SendAsync(
        HttpMethod method,
        string endpoint,
        string? body = null,
        CancellationToken cancellationToken = default);
}
