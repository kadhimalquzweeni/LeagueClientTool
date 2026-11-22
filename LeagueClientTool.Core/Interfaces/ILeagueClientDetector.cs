using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueClientDetector
{
    Task<LeagueClientConnectionInfo?> DetectAsync(CancellationToken cancellationToken = default);
}
