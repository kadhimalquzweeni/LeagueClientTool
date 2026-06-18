using LoLClientTool.Models;

namespace LoLClientTool.Services
{
    public interface IFriendToolsService
    {
        Task<LeagueClientResult> AcceptAllFriendRequestsAsync();

        Task<LeagueClientResult> DeleteAllFriendRequestsAsync();

        Task<List<FriendGroupOptionViewModel>> GetFriendGroupsAsync();

        Task<LeagueClientResult> DeleteFriendsFromGroupAsync(int groupId);
    }
}