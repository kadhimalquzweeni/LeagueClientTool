using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface IFriendToolsService
{
    Task<LeagueClientOperationResult> AcceptAllFriendRequestsAsync(CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> DeleteAllFriendRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FriendGroup>> GetFriendGroupsAsync(CancellationToken cancellationToken = default);
    Task<LeagueClientOperationResult> DeleteFriendsFromGroupAsync(int groupId, CancellationToken cancellationToken = default);
}
