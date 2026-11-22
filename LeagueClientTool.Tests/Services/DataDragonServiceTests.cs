using System.Net;
using System.Text;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class DataDragonServiceTests
{
    [Fact]
    public async Task GetLatestVersionAsync_CachesSuccessfulVersion()
    {
        int requestCount = 0;
        using var client = CreateClient(_ =>
        {
            requestCount++;
            return JsonResponse("[\\"16.22.1\\",\\"16.21.1\\"]");
        });
        var service = new DataDragonService(client);

        string first = await service.GetLatestVersionAsync();
        string second = await service.GetLatestVersionAsync();

        Assert.Equal("16.22.1", first);
        Assert.Equal(first, second);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task GetLatestVersionAsync_UsesFallbackWhenVersionEndpointFails()
    {
        using var client = CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = new DataDragonService(client);

        string version = await service.GetLatestVersionAsync();

        Assert.Equal("15.24.1", version);
    }

    [Fact]
    public async Task GetChampionBackgroundsAsync_MapsBaseChampionArtworkAndSortsByName()
    {
        using var client = CreateClient(request =>
        {
            if (request.RequestUri!.AbsoluteUri.Contains("/api/versions.json"))
            {
                return JsonResponse("[\\"16.22.1\\"]");
            }

            return JsonResponse("""
                {
                  "data": {
                    "Zed": { "id": "Zed", "key": "238", "name": "Zed" },
                    "Ahri": { "id": "Ahri", "key": " fox ", "name": "Ahri" },
                    "Lux": { "id": "Lux", "key": "99", "name": "Lux" }
                  }
                }
                """);
        });
        var service = new DataDragonService(client);

        var champions = await service.GetChampionBackgroundsAsync();

        Assert.Equal(new[] { "Lux", "Zed" }, champions.Select(champion => champion.ChampionName));
        Assert.Equal(99000, champions[0].SkinId);
        Assert.Equal("https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Lux_0.jpg",
            champions[0].ImageUrl);
    }

    [Fact]
    public async Task GetChampionSkinsAsync_ExcludesVariantSkinsAndMapsBaseSkinName()
    {
        using var client = CreateClient(request =>
        {
            if (request.RequestUri!.AbsoluteUri.Contains("/api/versions.json"))
            {
                return JsonResponse("[\\"16.22.1\\"]");
            }

            return JsonResponse("""
                {
                  "data": {
                    "Ahri": {
                      "id": "Ahri",
                      "name": "Ahri",
                      "skins": [
                        { "id": "103000", "num": 0, "name": "default" },
                        { "id": "103001", "num": 1, "name": "Dynasty Ahri" },
                        { "id": "103002", "num": 2, "name": "Variant", "parentSkin": 103001 }
                      ]
                    }
                  }
                }
                """);
        });
        var service = new DataDragonService(client);

        var skins = await service.GetChampionSkinsAsync("Ahri");

        Assert.Equal(2, skins.Count);
        Assert.Equal("Base", skins[0].SkinName);
        Assert.Equal(103001, skins[1].SkinId);
        Assert.Equal("https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Ahri_1.jpg",
            skins[1].ImageUrl);
    }

    [Theory]
    [InlineData("../Ahri")]
    [InlineData("Ahri/../../other")]
    [InlineData(@"Ahri\Other")]
    public async Task GetChampionSkinsAsync_RejectsUnsafeChampionIds(string championId)
    {
        using var client = CreateClient(_ => throw new InvalidOperationException("No request expected."));
        var service = new DataDragonService(client);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetChampionSkinsAsync(championId));
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new StubHttpMessageHandler(handler));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
