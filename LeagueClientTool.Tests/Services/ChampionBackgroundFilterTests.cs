using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class ChampionBackgroundFilterTests
{
    private static readonly ChampionBackground[] Champions =
    [
        new(1, "Aatrox", "Aatrox", "aatrox.jpg"),
        new(2, "Ahri", "Ahri", "ahri.jpg"),
        new(3, "MissFortune", "Miss Fortune", "missfortune.jpg"),
        new(4, "LeeSin", "Lee Sin", "leesin.jpg")
    ];

    [Fact]
    public void Filter_WhenSearchIsEmpty_ReturnsAllChampionsInOriginalOrder()
    {
        var result = ChampionBackgroundFilter.Filter(Champions, string.Empty);

        Assert.Equal(Champions, result);
    }

    [Fact]
    public void Filter_MatchesChampionNamesWithoutCaseSensitivity()
    {
        var result = ChampionBackgroundFilter.Filter(Champions, "aHrI");

        Assert.Collection(result, champion => Assert.Equal("Ahri", champion.ChampionName));
    }

    [Fact]
    public void Filter_TrimsSearchTextAndMatchesNamesContainingTheQuery()
    {
        var result = ChampionBackgroundFilter.Filter(Champions, "  fortune  ");

        Assert.Collection(result, champion => Assert.Equal("Miss Fortune", champion.ChampionName));
    }

    [Fact]
    public void Filter_ReturnsEmptyWhenNoChampionMatches()
    {
        var result = ChampionBackgroundFilter.Filter(Champions, "Teemo");

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_RejectsNullChampionCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChampionBackgroundFilter.Filter(null!, "Ahri"));
    }

    [Fact]
    public void Filter_DoesNotModifyOriginalCollection()
    {
        var source = Champions.ToList();

        ChampionBackgroundFilter.Filter(source, "Lee");

        Assert.Equal(Champions, source);
    }
}
