using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Retrieves champion and skin artwork from Riot's public Data Dragon service.
/// Version lookup is cached for six hours and falls back to a known stable version
/// when Riot's version endpoint is temporarily unavailable.
/// </summary>
public sealed class DataDragonService(HttpClient httpClient) : IDataDragonService
{
    private const string VersionsEndpoint = "https://ddragon.leagueoflegends.com/api/versions.json";
    private const string ChampionEndpoint = "https://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion.json";
    private const string ChampionDetailsEndpoint = "https://ddragon.leagueoflegends.com/cdn/{0}/data/en_US/champion/{1}.json";
    private const string FallbackVersion = "15.24.1";
    private static readonly TimeSpan VersionCacheDuration = TimeSpan.FromHours(6);

    private readonly SemaphoreSlim _versionLock = new(1, 1);
    private string? _cachedVersion;
    private DateTimeOffset _versionExpiresAt;

    public async Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedVersion is not null && DateTimeOffset.UtcNow < _versionExpiresAt)
        {
            return _cachedVersion;
        }

        await _versionLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedVersion is not null && DateTimeOffset.UtcNow < _versionExpiresAt)
            {
                return _cachedVersion;
            }

            try
            {
                var versions = await httpClient.GetFromJsonAsync<List<string>>(
                    VersionsEndpoint, cancellationToken);

                _cachedVersion = versions?.FirstOrDefault();
            }
            catch (HttpRequestException)
            {
                _cachedVersion = null;
            }
            catch (JsonException)
            {
                _cachedVersion = null;
            }

            _cachedVersion ??= FallbackVersion;
            _versionExpiresAt = DateTimeOffset.UtcNow.Add(VersionCacheDuration);
            return _cachedVersion;
        }
        finally
        {
            _versionLock.Release();
        }
    }

    public async Task<IReadOnlyList<ChampionBackground>> GetChampionBackgroundsAsync(
        CancellationToken cancellationToken = default)
    {
        string version = await GetLatestVersionAsync(cancellationToken);
        string endpoint = string.Format(ChampionEndpoint, version);

        var response = await httpClient.GetFromJsonAsync<DataDragonChampionResponse>(
            endpoint, cancellationToken);

        if (response?.Data is null)
        {
            return [];
        }

        return response.Data.Values
            .Select(champion =>
            {
                if (!int.TryParse(champion.Key, out int championKey))
                {
                    return null;
                }

                return new ChampionBackground(
                    championKey * 1000,
                    champion.Id,
                    champion.Name,
                    $"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{champion.Id}_0.jpg");
            })
            .Where(champion => champion is not null)
            .Cast<ChampionBackground>()
            .OrderBy(champion => champion.ChampionName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<ChampionSkin>> GetChampionSkinsAsync(
        string championId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(championId);

        if (championId.Contains('/') || championId.Contains('\\') ||
            championId.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("A valid Data Dragon champion ID is required.", nameof(championId));
        }

        string version = await GetLatestVersionAsync(cancellationToken);
        string endpoint = string.Format(ChampionDetailsEndpoint, version, Uri.EscapeDataString(championId));

        var response = await httpClient.GetFromJsonAsync<DataDragonChampionDetailsResponse>(
            endpoint, cancellationToken);

        if (response?.Data is null ||
            !response.Data.TryGetValue(championId, out var champion) ||
            champion.Skins is null)
        {
            return [];
        }

        return champion.Skins
            .Where(skin => skin.ParentSkin is null)
            .Select(skin =>
            {
                if (!int.TryParse(skin.Id, out int skinId))
                {
                    return null;
                }

                return new ChampionSkin(
                    skinId,
                    skin.Num,
                    champion.Id,
                    champion.Name,
                    string.Equals(skin.Name, "default", StringComparison.OrdinalIgnoreCase)
                        ? "Base"
                        : skin.Name,
                    $"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{champion.Id}_{skin.Num}.jpg");
            })
            .Where(skin => skin is not null)
            .Cast<ChampionSkin>()
            .ToArray();
    }

    private sealed class DataDragonChampionResponse
    {
        [JsonPropertyName("data")]
        public Dictionary<string, DataDragonChampion> Data { get; init; } = [];
    }

    private sealed class DataDragonChampion
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class DataDragonChampionDetailsResponse
    {
        [JsonPropertyName("data")]
        public Dictionary<string, DataDragonChampionDetails> Data { get; init; } = [];
    }

    private sealed class DataDragonChampionDetails
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("skins")]
        public List<DataDragonSkin> Skins { get; init; } = [];
    }

    private sealed class DataDragonSkin
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("num")]
        public int Num { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("parentSkin")]
        public int? ParentSkin { get; init; }
    }
}
