using System.Text.Json;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class FriendToolsServiceTests
{
    [Fact]
    public async Task AcceptAllFriendRequestsAsync_AcceptsOnlyIncomingRequests()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponses["/lol-chat/v2/friend-requests"] = """
            [
              { "puuid": "incoming-1", "direction": "in", "name": "Player" },
              { "puuid": "outgoing-1", "direction": "out", "name": "Other" }
            ]
            """;

        var result = await fixture.Service.AcceptAllFriendRequestsAsync();

        Assert.True(result.Success);
        Assert.Single(fixture.Api.Requests);
        Assert.Equal(HttpMethod.Put, fixture.Api.Requests[0].Method);
        Assert.Equal("/lol-chat/v2/friend-requests/incoming-1", fixture.Api.Requests[0].Endpoint);
        var payload = JsonDocument.Parse(fixture.Api.Requests[0].Body!).RootElement;
        Assert.Equal("incoming-1", payload.GetProperty("id").GetString());
        Assert.Equal(string.Empty, payload.GetProperty("note").GetString());
    }

    [Fact]
    public async Task DeleteAllFriendRequestsAsync_DeletesEveryRequest()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponses["/lol-chat/v2/friend-requests"] = """
            [{ "id": "one" }, { "id": "two" }]
            """;

        var result = await fixture.Service.DeleteAllFriendRequestsAsync();

        Assert.True(result.Success);
        Assert.Equal(2, fixture.Api.Requests.Count);
        Assert.All(fixture.Api.Requests, request => Assert.Equal(HttpMethod.Delete, request.Method));
    }

    [Fact]
    public async Task GetFriendGroupsAsync_IncludesGeneralAndSortedCustomGroups()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponses["/lol-chat/v1/friend-groups"] = """
            [
              { "id": 9, "name": "Zeta" },
              { "id": 3, "name": "Alpha" }
            ]
            """;

        var groups = await fixture.Service.GetFriendGroupsAsync();

        Assert.Equal(new[] { "General", "Alpha", "Zeta" }, groups.Select(group => group.Name));
        Assert.Equal(0, groups[0].GroupId);
    }

    [Fact]
    public async Task DeleteFriendsFromGroupAsync_DeletesOnlySelectedGroup()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponses["/lol-chat/v1/friends"] = """
            [
              { "id": "friend-a", "groupId": 4 },
              { "id": "friend-b", "groupId": 5 },
              { "id": "friend-c", "groupId": 4 }
            ]
            """;

        var result = await fixture.Service.DeleteFriendsFromGroupAsync(4);

        Assert.True(result.Success);
        Assert.Equal(2, fixture.Api.Requests.Count);
        Assert.Contains(fixture.Api.Requests, request => request.Endpoint == "/lol-chat/v1/friends/friend-a");
        Assert.Contains(fixture.Api.Requests, request => request.Endpoint == "/lol-chat/v1/friends/friend-c");
    }

    [Fact]
    public async Task DeleteFriendsFromGroupAsync_WhenNoFriendsMatchDoesNotSendDeletes()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponses["/lol-chat/v1/friends"] = """[{ "id": "friend-a", "groupId": 4 }]""";

        var result = await fixture.Service.DeleteFriendsFromGroupAsync(7);

        Assert.True(result.Success);
        Assert.Empty(fixture.Api.Requests);
    }

    [Fact]
    public async Task AcceptAllFriendRequestsAsync_WhenDisconnectedReturnsFailure()
    {
        var fixture = new Fixture(connected: false);

        var result = await fixture.Service.AcceptAllFriendRequestsAsync();

        Assert.False(result.Success);
        Assert.Null(fixture.Factory.CreatedClient);
    }

    [Fact]
    public async Task GetFriendGroupsAsync_WhenDisconnectedReturnsGeneralGroup()
    {
        var fixture = new Fixture(connected: false);

        var groups = await fixture.Service.GetFriendGroupsAsync();

        Assert.Single(groups);
        Assert.Equal(new FriendGroup(0, "General"), groups[0]);
    }

    private sealed class Fixture
    {
        public FakeApiClient Api { get; } = new();
        public FakeApiFactory Factory { get; }
        public FriendToolsService Service { get; }

        public Fixture(bool connected = true)
        {
            Factory = new FakeApiFactory(Api);
            Service = new FriendToolsService(new FakeDetector(connected), Factory);
        }
    }

    private sealed class FakeDetector(bool connected) : ILeagueClientDetector
    {
        public Task<LeagueClientConnectionInfo?> DetectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<LeagueClientConnectionInfo?>(
                connected ? new LeagueClientConnectionInfo(1, 54321, "secret", "https") : null);
    }

    private sealed class FakeApiFactory(FakeApiClient client) : ILeagueClientApiClientFactory
    {
        public FakeApiClient? CreatedClient { get; private set; }

        public ILeagueClientApiClient Create(LeagueClientConnectionInfo connectionInfo)
        {
            CreatedClient = client;
            return client;
        }
    }

    private sealed class FakeApiClient : ILeagueClientApiClient
    {
        public Dictionary<string, string> GetResponses { get; } = new();
        public List<CapturedRequest> Requests { get; } = new();

        public Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetResponses.TryGetValue(endpoint, out string? json) ? json : "[]");

        public Task<LeagueClientApiResponse> SendAsync(
            HttpMethod method,
            string endpoint,
            string? body = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(new CapturedRequest(method, endpoint, body));
            return Task.FromResult(new LeagueClientApiResponse(200, string.Empty));
        }

        public void Dispose() { }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Endpoint, string? Body);
}
