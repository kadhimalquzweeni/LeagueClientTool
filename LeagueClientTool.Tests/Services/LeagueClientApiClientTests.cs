using System.Net;
using System.Text;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class LeagueClientApiClientTests
{
    [Fact]
    public async Task GetAsync_SendsRelativeRequestAndReturnsResponseBody()
    {
        var handler = new StubHttpMessageHandler(
            request =>
            {
                Assert.Equal(
                    "https://127.0.0.1:54321/lol-summoner/v1/current-summoner",
                    request.RequestUri!.AbsoluteUri);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"summonerId\":123}")
                };
            });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://127.0.0.1:54321")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        var result = await apiClient.GetAsync("/lol-summoner/v1/current-summoner");

        Assert.Equal("{\"summonerId\":123}", result);
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("//example.com/path")]
    [InlineData(@"\\example.com\path")]
    public async Task GetAsync_AbsoluteOrNetworkPathIsRejected(string endpoint)
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => throw new InvalidOperationException("No request should be sent.")));
        using var apiClient = new LeagueClientApiClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() => apiClient.GetAsync(endpoint));
    }

    [Fact]
    public async Task GetAsync_NonSuccessStatusThrowsHttpRequestException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)))
        {
            BaseAddress = new Uri("https://127.0.0.1:54321/")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => apiClient.GetAsync("/lol-summoner/v1/current-summoner"));
    }

    [Fact]
    public void HttpClientFactory_ConfiguresLoopbackBaseAddressAndBasicAuthentication()
    {
        var connection = new LeagueClientConnectionInfo(123, 54321, "local-secret", "https");

        using var httpClient = LeagueClientHttpClientFactory.Create(connection);

        Assert.Equal("https://127.0.0.1:54321/", httpClient.BaseAddress!.AbsoluteUri);
        Assert.Equal("Basic", httpClient.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:local-secret")),
            httpClient.DefaultRequestHeaders.Authorization.Parameter);
        Assert.Equal(TimeSpan.FromSeconds(10), httpClient.Timeout);
    }

    [Fact]
    public async Task SendAsync_PutSendsJsonBodyAndReturnsStatusAndBody()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/lol-summoner/v1/current-summoner/icon", request.RequestUri!.AbsolutePath);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            Assert.Equal("{\"profileIconId\":12}", request.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            return new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                Content = new StringContent(string.Empty)
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://127.0.0.1:54321/")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        var result = await apiClient.SendAsync(
            HttpMethod.Put,
            "/lol-summoner/v1/current-summoner/icon",
            "{\"profileIconId\":12}");

        Assert.Equal(204, result.StatusCode);
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(string.Empty, result.Body);
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("//example.com/path")]
    [InlineData("/lol-client/../secrets")]
    [InlineData("/lol-client\\\\secrets")]
    [InlineData("/lol-client/path#fragment")]
    public async Task SendAsync_UnsafeEndpointIsRejected(string endpoint)
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => throw new InvalidOperationException("No request should be sent.")))
        {
            BaseAddress = new Uri("https://127.0.0.1:54321/")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(
            () => apiClient.SendAsync(HttpMethod.Get, endpoint));
    }

    [Fact]
    public async Task SendAsync_UnsupportedMethodIsRejected()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => throw new InvalidOperationException("No request should be sent.")))
        {
            BaseAddress = new Uri("https://127.0.0.1:54321/")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(
            () => apiClient.SendAsync(new HttpMethod("TRACE"), "/lol-summoner/v1/current-summoner"));
    }

    [Fact]
    public async Task SendAsync_NonSuccessStatusIsReturnedWithoutThrowing()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid payload")
            }))
        {
            BaseAddress = new Uri("https://127.0.0.1:54321/")
        };
        using var apiClient = new LeagueClientApiClient(httpClient);

        var result = await apiClient.SendAsync(HttpMethod.Post, "/lol-summoner/v1/current-summoner");

        Assert.Equal(400, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
        Assert.Equal("invalid payload", result.Body);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
