namespace LoLClientTool.Services
{
    public class LeagueClientResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
        
        public string ResponseBody { get; set; } = string.Empty;
    }
}