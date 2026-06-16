using System.ComponentModel.DataAnnotations; //imports validation attributes like [Required] and [Range]

namespace LoLClientTool.Models
{
    public class ProfileIconViewModel // a class used to send data between the Controller and the View
    {
        [Required(ErrorMessage = "Please enter a profile icon ID.")] //IconId is required the user must provide a value for it to be valid
        [Range(0, int.MaxValue, ErrorMessage = "Profile icon ID must be a positive number.")] //IconId must be a positive number greater than 0
        public int? IconId { get; set; } //the validation attributes above will be applied to this property

        public string? StatusMessage { get; set; } //this property will hold a status message to display to the user, such as success or error messages

        public bool IsLeagueClientRunning { get; set; }

        public List<ProfileIconOptionViewModel> AvailableIcons { get; set; } = new();
    }
}