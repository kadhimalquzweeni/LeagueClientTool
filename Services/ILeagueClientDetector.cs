namespace LoLClientTool.Mvc.Services
{
    public interface ILeagueClientDetector
    {
        bool IsLeagueClientRunning();

        LeagueClientConnection? GetConnection();
    }
}