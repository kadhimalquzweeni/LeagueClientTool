using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueProfileService
{
    Task<LeagueClientOperationResult> SetProfileIconAsync(int iconId, CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> SetProfileBackgroundAsync(int skinId, CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> UpdateStatusMessageAsync(string statusMessage, CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> ChangeRiotIdAsync(string gameName, string tagLine, CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> SetVisibleRankAsync(string queue, string tier, string division, CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> ClearVisibleRankAsync(CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> SetLastRankBannerAsync(CancellationToken cancellationToken = default);
}
