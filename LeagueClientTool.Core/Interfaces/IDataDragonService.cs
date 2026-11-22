using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface IDataDragonService
{
    Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChampionBackground>> GetChampionBackgroundsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChampionSkin>> GetChampionSkinsAsync(
        string championId,
        CancellationToken cancellationToken = default);
}
