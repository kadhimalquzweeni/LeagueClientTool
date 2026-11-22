using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

public static class ChampionBackgroundFilter
{
    public static IReadOnlyList<ChampionBackground> Filter(
        IEnumerable<ChampionBackground> champions,
        string? searchText)
    {
        ArgumentNullException.ThrowIfNull(champions);

        if (string.IsNullOrWhiteSpace(searchText))
            return champions.ToArray();

        return champions
            .Where(champion => champion.ChampionName.Contains(
                searchText.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
