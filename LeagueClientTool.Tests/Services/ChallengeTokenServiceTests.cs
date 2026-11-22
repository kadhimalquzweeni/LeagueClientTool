using System.Text.Json;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class ChallengeTokenServiceTests
{
    [Fact]
    public async Task ClearAsync_PostsEmptyChallengeArray()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.ClearAsync();

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, fixture.Api.LastMethod);
        Assert.Equal("/lol-challenges/v1/update-player-preferences/", fixture.Api.LastEndpoint);
        Assert.Empty(JsonDocument.Parse(fixture.Api.LastBody!).RootElement.GetProperty("challengeIds").EnumerateArray());
    }

    [Fact]
    public async Task CopyFirstTokenToAllSlotsAsync_CopiesFirstTokenAndPreservesTitleAndBanner()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponse = """
            {
              "topChallenges": [{ "id": "456" }, { "id": 789 }],
              "title": { "itemId": 123 },
              "bannerId": 7
            }
            """;

        var result = await fixture.Service.CopyFirstTokenToAllSlotsAsync();

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, fixture.Api.LastMethod);
        var payload = JsonDocument.Parse(fixture.Api.LastBody!).RootElement;
        Assert.Equal(new[] { 456, 456, 456 },
            payload.GetProperty("challengeIds").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal("123", payload.GetProperty("title").GetString());
        Assert.Equal("7", payload.GetProperty("bannerAccent").GetString());
    }

    [Fact]
    public async Task CopyFirstTokenToAllSlotsAsync_WhenNoTokenExistsReturnsHelpfulFailure()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponse = """{ "topChallenges": [] }""";

        var result = await fixture.Service.CopyFirstTokenToAllSlotsAsync();

        Assert.False(result.Success);
        Assert.Contains("No equipped challenge token", result.Message);
        Assert.Equal(HttpMethod.Get, fixture.Api.LastMethod);
        Assert.Equal("/lol-challenges/v1/summary-player-data/local-player", fixture.Api.LastEndpoint);
    }

    [Fact]
    public async Task CopyFirstTokenToAllSlotsAsync_WhenSummaryJsonIsInvalidReturnsFailure()
    {
        var fixture = new Fixture();
        fixture.Api.GetResponse = "not-json";

        var result = await fixture.Service.CopyFirstTokenToAllSlotsAsync();

        Assert.False(result.Success);
        Assert.Contains("invalid challenge data", result.Message);
    }

    [Fact]
    public async Task ClearAsync_WhenDisconnectedReturnsFailure()
    {
        var fixture = new Fixture(connected: false);

        var result = await fixture.Service.ClearAsync();

        Assert.False(result.Success);
        Assert.Null(fixture.Factory.CreatedClient);
    }

    private sealed class Fixture
    {
        public FakeApiClient Api { get; } = new();
        public FakeApiFactory Factory { get; }
        public ChallengeTokenService Service { get; }

        public Fixture(bool connected = true)
        {
            Factory = new FakeApiFactory(Api);
            Service = new ChallengeTokenService(new FakeDetector(connected), Factory);
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
        public string GetResponse { get; set; } = "{}";
        public LeagueClientApiResponse Response { get; set; } = new(200, "{}");

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
