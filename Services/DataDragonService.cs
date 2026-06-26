using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace LoLClientTool.Services
{
    public class DataDragonService : IDataDragonService
    {
        private const string VersionsUrl = "https://ddragon.leagueoflegends.com/api/versions.json";
        private const string FallbackVersion = "15.24.1";
        private const string CacheKey = "LatestDataDragonVersion";

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;

        public DataDragonService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetLatestVersionAsync()
        {
            string? version = await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

                try
                {
                    List<string>? versions = await _httpClient.GetFromJsonAsync<List<string>>(VersionsUrl);

                    if (versions == null || versions.Count == 0)
                    {
                        return FallbackVersion;
                    }

                    return versions[0];
                }
                catch
                {
                    return FallbackVersion;
                }
            });

            return version ?? FallbackVersion;
        }
    }
}