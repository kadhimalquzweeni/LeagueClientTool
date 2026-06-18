namespace LoLClientTool.Models
{
    public class FriendToolsViewModel
    {
        public string? ResultMessage { get; set; }

        public bool IsLeagueClientRunning { get; set; }

        public int? SelectedGroupId { get; set; }

        public List<FriendGroupOptionViewModel> FriendGroups { get; set; } = new();
    }
}