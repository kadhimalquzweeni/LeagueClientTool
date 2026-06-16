namespace LoLClientTool.Mvc.Services
{
    public class LeagueClientConnection
    {
        public int Port { get; set; }

        public string Password { get; set; } = string.Empty;

        public string Protocol { get; set; } = string.Empty;
    }
}