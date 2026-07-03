using System.ComponentModel.DataAnnotations;

namespace LoLClientTool.Models
{
    public class CustomRequestViewModel
    {
        public bool IsLeagueClientRunning { get; set; }

        public string ResultMessage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a request method.")]
        public string Method { get; set; } = "GET";

        [Required(ErrorMessage = "Enter an LCU endpoint.")]
        public string Endpoint { get; set; } = string.Empty;

        public string? Body { get; set; }

        public string? ResponseBody { get; set; }

        public int? StatusCode { get; set; }

        public bool RequestSucceeded { get; set; }
    }
}