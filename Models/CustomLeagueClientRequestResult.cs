namespace LoLClientTool.Models
{
    public class CustomLeagueClientRequestResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string ResponseBody { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}