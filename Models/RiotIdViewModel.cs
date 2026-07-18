using System.ComponentModel.DataAnnotations;

namespace LoLClientTool.Models
{
    public class RiotIdViewModel
    {
        public bool IsLeagueClientRunning { get; set; }

        public string ResultMessage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a Riot ID name.")]
        public string? GameName { get; set; }

        [Required(ErrorMessage = "Enter a tagline.")]
        public string? TagLine { get; set; }
        public string? ResponseBody { get; set; }
    }
}