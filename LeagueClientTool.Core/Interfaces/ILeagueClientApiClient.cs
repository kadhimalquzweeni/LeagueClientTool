namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueClientApiClient : IDisposable
{
    Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default);
}
