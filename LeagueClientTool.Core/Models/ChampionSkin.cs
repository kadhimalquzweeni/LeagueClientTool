namespace LeagueClientTool.Core.Models;

public sealed record ChampionSkin(
    int SkinId,
    int SkinNumber,
    string ChampionId,
    string ChampionName,
    string SkinName,
    string ImageUrl);
