using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueCustomRequestService
{
    Task<LeagueClientOperationResult> SendAsync(
        string method,
        string endpoint,
        string? body = null,
        CancellationToken cancellationToken = default);
}
