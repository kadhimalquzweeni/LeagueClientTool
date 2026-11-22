using System.Text.Json;
using System.Text.Json.Nodes;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Manages friend requests, friend folders and friends through the local League chat API.
/// Bulk operations report partial failures instead of silently discarding them.
/// </summary>
public sealed class FriendToolsService(
    ILeagueClientDetector detector,
    ILeagueClientApiClientFactory apiClientFactory) : IFriendToolsService
{
    private const string FriendRequestsEndpoint = "/lol-chat/v2/friend-requests";
    private const string FriendGroupsEndpoint = "/lol-chat/v1/friend-groups";
    private const string FriendsEndpoint = "/lol-chat/v1/friends";

    public async Task<LeagueClientOperationResult> AcceptAllFriendRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null) return NotConnected();

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        var requestsResult = await ReadArrayAsync(api, FriendRequestsEndpoint, cancellationToken);
        if (!requestsResult.Success)
        {
            return new LeagueClientOperationResult(false, requestsResult.Error!, requestsResult.StatusCode, requestsResult.Body);
        }

        JsonElement[] incoming = requestsResult.Items!
            .Where(IsIncomingRequest)
            .ToArray();

        if (incoming.Length == 0)
        {
            return new LeagueClientOperationResult(true, "No incoming friend requests were found.");
        }

        int accepted = 0;
        var errors = new List<string>();

        foreach (JsonElement request in incoming)
        {
            string? requestId = GetRequestId(request, "puuid", "pid", "id");
            if (string.IsNullOrWhiteSpace(requestId)) continue;

            JsonObject payload = JsonNode.Parse(request.GetRawText()) as JsonObject ?? new JsonObject();
            payload["id"] = requestId;
            payload["note"] ??= string.Empty;

            LeagueClientApiResponse response = await api.SendAsync(
                HttpMethod.Put,
                $"{FriendRequestsEndpoint}/{Uri.EscapeDataString(requestId)}",
                payload.ToJsonString(),
                cancellationToken);

            if (response.IsSuccessStatusCode) accepted++;
            else errors.Add($"{requestId}: HTTP {response.StatusCode} {response.Body}");
        }

        return BuildBatchResult(accepted, errors, "friend request(s)", "Accepted");
    }

    public async Task<LeagueClientOperationResult> DeleteAllFriendRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null) return NotConnected();

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        var requestsResult = await ReadArrayAsync(api, FriendRequestsEndpoint, cancellationToken);
        if (!requestsResult.Success)
        {
            return new LeagueClientOperationResult(false, requestsResult.Error!, requestsResult.StatusCode, requestsResult.Body);
        }

        return await DeleteItemsAsync(
            api,
            requestsResult.Items!,
            request => GetRequestId(request, "puuid", "pid", "id"),
            FriendRequestsEndpoint,
            "friend request(s)",
            cancellationToken);
    }

    public async Task<IReadOnlyList<FriendGroup>> GetFriendGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = new List<FriendGroup> { new(0, "General") };
        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null) return groups;

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        var result = await ReadArrayAsync(api, FriendGroupsEndpoint, cancellationToken);
        if (!result.Success) return groups;

        groups.AddRange(result.Items!
            .Select(item => new FriendGroup(
                ReadInt32(item, "id") ?? -1,
                ReadString(item, "name") ?? "Unnamed group"))
            .Where(group => group.GroupId > 0)
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase));

        return groups;
    }

    public async Task<LeagueClientOperationResult> DeleteFriendsFromGroupAsync(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        if (groupId < 0) throw new ArgumentOutOfRangeException(nameof(groupId));

        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null) return NotConnected();

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        var friendsResult = await ReadArrayAsync(api, FriendsEndpoint, cancellationToken);
        if (!friendsResult.Success)
        {
            return new LeagueClientOperationResult(false, friendsResult.Error!, friendsResult.StatusCode, friendsResult.Body);
        }

        JsonElement[] selected = friendsResult.Items!
            .Where(friend => ReadInt32(friend, "groupId") == groupId)
            .ToArray();

        if (selected.Length == 0)
        {
            return new LeagueClientOperationResult(true, "No friends were found in the selected folder.");
        }

        return await DeleteItemsAsync(
            api,
            selected,
            friend => GetRequestId(friend, "id", "puuid", "pid"),
            FriendsEndpoint,
            "friend(s) from the selected folder",
            cancellationToken);
    }

    private static async Task<LeagueClientOperationResult> DeleteItemsAsync(
        ILeagueClientApiClient api,
        IReadOnlyList<JsonElement> items,
        Func<JsonElement, string?> getId,
        string endpoint,
        string itemDescription,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return new LeagueClientOperationResult(true, $"No {itemDescription} were found.");
        }

        int deleted = 0;
        var errors = new List<string>();

        foreach (JsonElement item in items)
        {
            string? id = getId(item);
            if (string.IsNullOrWhiteSpace(id)) continue;

            LeagueClientApiResponse response = await api.SendAsync(
                HttpMethod.Delete,
                $"{endpoint}/{Uri.EscapeDataString(id)}",
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode) deleted++;
            else errors.Add($"{id}: HTTP {response.StatusCode} {response.Body}");
        }

        return BuildBatchResult(deleted, errors, itemDescription, "Deleted");
    }

    private static async Task<ArrayReadResult> ReadArrayAsync(
        ILeagueClientApiClient api,
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            string json = await api.GetAsync(endpoint, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new(false, [], "The League Client returned an unexpected response format.", null, json);
            }

            return new(true, document.RootElement.EnumerateArray().Select(x => x.Clone()).ToArray(), null, null, json);
        }
        catch (HttpRequestException exception)
        {
            return new(false, [], "The League Client request failed.", (int?)exception.StatusCode, exception.Message);
        }
        catch (JsonException exception)
        {
            return new(false, [], "The League Client returned invalid JSON.", null, exception.Message);
        }
    }

    private static bool IsIncomingRequest(JsonElement request)
    {
        string? direction = ReadString(request, "direction");
        return !string.Equals(direction, "out", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(direction, "outgoing", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetRequestId(JsonElement element, params string[] properties)
    {
        foreach (string property in properties)
        {
            string? value = ReadString(element, property);
            if (!string.IsNullOrWhiteSpace(value) && value != "0") return value;
        }

        return null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static int? ReadInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)) return number;
        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out int parsed)
            ? parsed
            : null;
    }

    private static LeagueClientOperationResult BuildBatchResult(
        int succeeded,
        IReadOnlyList<string> errors,
        string description,
        string action)
    {
        if (errors.Count > 0)
        {
            return new LeagueClientOperationResult(
                false,
                $"{action} {succeeded} {description}, but {errors.Count} operation(s) failed: {string.Join(" | ", errors)}");
        }

        return new LeagueClientOperationResult(true, $"{action} {succeeded} {description}.");
    }

    private static LeagueClientOperationResult NotConnected() =>
        new(false, "League Client is not running, or the lockfile could not be read.");

    private sealed record ArrayReadResult(
        bool Success,
        IReadOnlyList<JsonElement> Items,
        string? Error,
        int? StatusCode,
        string? Body);
}
