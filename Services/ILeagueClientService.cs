namespace LoLClientTool.Services
{
    public interface ILeagueClientService
    {
        Task<LeagueClientResult> SetProfileIconAsync(int iconId);
    }
}