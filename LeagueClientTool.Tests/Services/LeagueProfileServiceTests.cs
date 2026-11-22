using System.Text.Json;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class LeagueProfileServiceTests
{
    [Fact]
    public async Task SetProfileIconAsync_SendsPutWithIconPayload()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.SetProfileIconAsync(42);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Put, fixture.Api.LastMethod);
        Assert.Equal("/lol-summoner/v1/current-summoner/icon", fixture.Api.LastEndpoint);
        Assert.Equal(42, JsonDocument.Parse(fixture.Api.LastBody!).RootElement.GetProperty("profileIconId").GetInt32());
    }

    [Fact]
    public async Task SetProfileBackgroundAsync_SendsPostWithBackgroundPreference()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.SetProfileBackgroundAsync(103001);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, fixture.Api.LastMethod);
        Assert.Equal("/lol-summoner/v1/current-summoner/summoner-profile", fixture.Api.LastEndpoint);
        var payload = JsonDocument.Parse(fixture.Api.LastBody!).RootElement;
        Assert.Equal("backgroundSkinId", payload.GetProperty("key").GetString());
        Assert.Equal(103001, payload.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task UpdateStatusMessageAsync_SendsStatusMessage()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.UpdateStatusMessageAsync("Playing with friends");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Put, fixture.Api.LastMethod);
        Assert.Equal("/lol-chat/v1/me", fixture.Api.LastEndpoint);
        Assert.Equal("Playing with friends",
            JsonDocument.Parse(fixture.Api.LastBody!).RootElement.GetProperty("statusMessage").GetString());
    }

    [Fact]
    public async Task ChangeRiotIdAsync_TrimsNameAndTagline()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.ChangeRiotIdAsync("  Summoner  ", " EUW ");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, fixture.Api.LastMethod);
        Assert.Equal("/lol-summoner/v1/save-alias", fixture.Api.LastEndpoint);
        var payload = JsonDocument.Parse(fixture.Api.LastBody!).RootElement;
        Assert.Equal("Summoner", payload.GetProperty("gameName").GetString());
        Assert.Equal("EUW", payload.GetProperty("tagLine").GetString());
    }

    [Fact]
    public async Task SetVisibleRankAsync_SendsRankFields()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.SetVisibleRankAsync("RANKED_SOLO_5x5", "GOLD", "II");

        Assert.True(result.Success);
        var lol = JsonDocument.Parse(fixture.Api.LastBody!).RootElement.GetProperty("lol");
        Assert.Equal("RANKED_SOLO_5x5", lol.GetProperty("rankedLeagueQueue").GetString());
        Assert.Equal("GOLD", lol.GetProperty("rankedLeagueTier").GetString());
        Assert.Equal("II", lol.GetProperty("rankedLeagueDivision").GetString());
    }

    [Fact]
    public async Task ClearVisibleRankAsync_SendsEmptyRankFields()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.ClearVisibleRankAsync();

        Assert.True(result.Success);
        var lol = JsonDocument.Parse(fixture.Api.LastBody!).RootElement.GetProperty("lol");
        Assert.Equal(string.Empty, lol.GetProperty("rankedLeagueQueue").GetString());
        Assert.Equal(string.Empty, lol.GetProperty("rankedLeagueTier").GetString());
        Assert.Equal(string.Empty, lol.GetProperty("rankedLeagueDivision").GetString());
    }

    [Fact]
    public async Task SetLastRankBannerAsync_PreservesChallengeSlotsAndTitle()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponse = """
            {
              "topChallenges": [{ "id": 101 }, { "id": "202" }],
              "title": { "itemId": 303 }
            }
            """;

        var result = await fixture.Service.SetLastRankBannerAsync();

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, fixture.Api.LastMethod);
        Assert.Equal("/lol-challenges/v1/update-player-preferences/", fixture.Api.LastEndpoint);
        var payload = JsonDocument.Parse(fixture.Api.LastBody!).RootElement;
        Assert.Equal("2", payload.GetProperty("bannerAccent").GetString());
        Assert.Equal(new[] { 101, 202 },
            payload.GetProperty("challengeIds").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal("303", payload.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SetProfileIconAsync_WhenDisconnectedReturnsFailureWithoutCreatingClient()
    {
        var fixture = new Fixture(connected: false);

        var result = await fixture.Service.SetProfileIconAsync(1);

        Assert.False(result.Success);
        Assert.Null(fixture.Factory.CreatedClient);
    }

    [Fact]
    public async Task SetProfileIconAsync_WhenLeagueClientRejectsRequestReturnsStatusAndBody()
    {
        var fixture = new Fixture();
        fixture.Api.Response = new LeagueClientApiResponse(400, "invalid icon");

        var result = await fixture.Service.SetProfileIconAsync(999999);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("invalid icon", result.ResponseBody);
    }

    [Theory]
    [InlineData(-1)]
    public async Task SetProfileIconAsync_RejectsNegativeIconIds(int iconId)
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.SetProfileIconAsync(iconId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateStatusMessageAsync_RejectsEmptyMessages(string message)
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.UpdateStatusMessageAsync(message));
    }

    [Fact]
    public async Task UpdateStatusMessageAsync_RejectsMessagesLongerThanFiveThousandCharacters()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateStatusMessageAsync(new string('x', 5001)));
    }

    [Fact]
    public async Task ChangeRiotIdAsync_RejectsEmptyValues()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.ChangeRiotIdAsync(" ", "EUW"));
        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.ChangeRiotIdAsync("Name", " "));
    }

    private sealed class Fixture
    {
        public FakeApiClient Api { get; } = new();
        public FakeApiFactory Factory { get; }
        public LeagueProfileService Service { get; }

        public Fixture(bool connected = true)
        {
            Factory = new FakeApiFactory(Api);
            Service = new LeagueProfileService(
                new FakeDetector(connected),
                Factory);
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
        public HttpMethod? LastMethod { get; private set; }
        public string? LastEndpoint { get; private set; }
        public string? LastBody { get; private set; }
        public LeagueClientApiResponse Response { get; set; } = new(200, "{}");
        public string GetResponse { get; set; } = "{}";

        public Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            LastMethod = HttpMethod.Get;
            LastEndpoint = endpoint;
            return Task.FromResult(GetResponse);
        }

        public Task<LeagueClientApiResponse> SendAsync(
            HttpMethod method,
            string endpoint,
            string? body = null,
            CancellationToken cancellationToken = default)
        {
            LastMethod = method;
            LastEndpoint = endpoint;
            LastBody = body;
            return Task.FromResult(Response);
        }

        public void Dispose() { }
    }
}
