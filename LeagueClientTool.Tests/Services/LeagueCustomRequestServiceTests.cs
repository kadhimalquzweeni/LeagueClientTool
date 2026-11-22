using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class LeagueCustomRequestServiceTests
{
    [Fact]
    public async Task SendAsync_ForwardsMethodEndpointAndBody()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.SendAsync("PATCH", "/lol-chat/v1/me", "{\"statusMessage\":\"hello\"}");

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Patch, fixture.Api.LastMethod);
        Assert.Equal("/lol-chat/v1/me", fixture.Api.LastEndpoint);
        Assert.Equal("{\"statusMessage\":\"hello\"}", fixture.Api.LastBody);
    }

    [Fact]
    public async Task SendAsync_ReturnsResponseStatusAndBody()
    {
        var fixture = new Fixture();
        fixture.Api.Response = new LeagueClientApiResponse(404, "not found");

        var result = await fixture.Service.SendAsync("GET", "/lol-chat/v1/missing");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("not found", result.ResponseBody);
    }

    [Fact]
    public async Task SendAsync_WhenDisconnectedDoesNotCreateApiClient()
    {
        var fixture = new Fixture(connected: false);

        var result = await fixture.Service.SendAsync("GET", "/lol-chat/v1/me");

        Assert.False(result.Success);
        Assert.Null(fixture.Factory.CreatedClient);
    }

    [Theory]
    [InlineData("TRACE")]
    [InlineData("CONNECT")]
    [InlineData("")]
    public async Task SendAsync_RejectsUnsupportedMethods(string method)
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.SendAsync(method, "/lol-chat/v1/me"));
    }

    private sealed class Fixture
    {
        public FakeApiClient Api { get; } = new();
        public FakeApiFactory Factory { get; }
        public LeagueCustomRequestService Service { get; }

        public Fixture(bool connected = true)
        {
            Factory = new FakeApiFactory(Api);
            Service = new LeagueCustomRequestService(new FakeDetector(connected), Factory);
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

        public Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default) =>
            Task.FromResult("{}");

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
