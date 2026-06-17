using System.ComponentModel.DataAnnotations;

namespace LoLClientTool.Models
{
    public class ProfileStatusViewModel
    {
        [Required(ErrorMessage = "Please enter a status message.")]
        [StringLength(5000, ErrorMessage = "Status message must be 5000 characters or fewer.")]
        [DataType(DataType.MultilineText)]
        public string? StatusMessageInput { get; set; }

        public string? ResultMessage { get; set; }

        public bool IsLeagueClientRunning { get; set; }
    }
}