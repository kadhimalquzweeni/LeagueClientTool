using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface IChallengeTokenService
{
    Task<LeagueClientOperationResult> ClearAsync(CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> CopyFirstTokenToAllSlotsAsync(CancellationToken cancellationToken = default);
}
