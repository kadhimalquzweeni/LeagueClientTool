using System.Text.Json;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Implements the profile customisation actions previously exposed by the MVC application.
/// All requests are sent through the authenticated, loopback-only League Client API client.
/// </summary>
public sealed class LeagueProfileService(
    ILeagueClientDetector detector,
    ILeagueClientApiClientFactory apiClientFactory) : ILeagueProfileService
{
    public Task<LeagueClientOperationResult> SetProfileIconAsync(
        int iconId,
        CancellationToken cancellationToken = default)
    {
        if (iconId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iconId), "Profile icon ID cannot be negative.");
        }

        return SendAsync(
            HttpMethod.Put,
            "/lol-summoner/v1/current-summoner/icon",
            new { profileIconId = iconId },
            $"Profile icon changed to {iconId}.",
            cancellationToken);
    }

    public Task<LeagueClientOperationResult> SetProfileBackgroundAsync(
        int skinId,
        CancellationToken cancellationToken = default)
    {
        if (skinId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skinId), "Background skin ID must be positive.");
        }

        return SendAsync(
            HttpMethod.Post,
            "/lol-summoner/v1/current-summoner/summoner-profile",
            new { key = "backgroundSkinId", value = skinId },
            $"Profile background changed to skin ID {skinId}.",
            cancellationToken);
    }

    public Task<LeagueClientOperationResult> UpdateStatusMessageAsync(
        string statusMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statusMessage);

        return SendAsync(
            HttpMethod.Put,
            "/lol-chat/v1/me",
            new { statusMessage },
            "Profile status message updated successfully.",
            cancellationToken);
    }

    public Task<LeagueClientOperationResult> ChangeRiotIdAsync(
        string gameName,
        string tagLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagLine);

        return SendAsync(
            HttpMethod.Post,
            "/lol-summoner/v1/save-alias",
            new { gameName = gameName.Trim(), tagLine = tagLine.Trim() },
            "Riot ID updated successfully.",
            cancellationToken);
    }

    public Task<LeagueClientOperationResult> SetVisibleRankAsync(
        string queue,
        string tier,
        string division,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(tier);
        ArgumentException.ThrowIfNullOrWhiteSpace(division);

        return SendAsync(
            HttpMethod.Put,
            "/lol-chat/v1/me",
            new
            {
                lol = new
                {
                    rankedLeagueQueue = queue,
                    rankedLeagueTier = tier,
                    rankedLeagueDivision = division
                }
            },
            $"Visible rank changed to {tier} {division} for {queue}.",
            cancellationToken);
    }

    public Task<LeagueClientOperationResult> ClearVisibleRankAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Put,
            "/lol-chat/v1/me",
            new
            {
                lol = new
                {
                    rankedLeagueQueue = string.Empty,
                    rankedLeagueTier = string.Empty,
                    rankedLeagueDivision = string.Empty
                }
            },
            "Visible rank cleared.",
            cancellationToken);

    public async Task<LeagueClientOperationResult> SetLastRankBannerAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null)
        {
            return NotConnected();
        }

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        string summaryJson;

        try
        {
            summaryJson = await api.GetAsync(
                "/lol-challenges/v1/summary-player-data/local-player",
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return new LeagueClientOperationResult(
                false,
                "Could not read current profile preferences.",
                (int?)exception.StatusCode,
                exception.Message);
        }

        using JsonDocument summary = JsonDocument.Parse(summaryJson);
        JsonElement root = summary.RootElement;
        var payload = new Dictionary<string, object?>
        {
            ["bannerAccent"] = "2"
        };

        var challengeIds = new List<int>();
        if (root.TryGetProperty("topChallenges", out JsonElement topChallenges) &&
            topChallenges.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement challenge in topChallenges.EnumerateArray())
            {
                if (challenge.TryGetProperty("id", out JsonElement id) &&
                    TryReadInt32(id, out int challengeId))
                {
                    challengeIds.Add(challengeId);
                }
            }
        }

        if (challengeIds.Count > 0)
        {
            payload["challengeIds"] = challengeIds;
        }

        if (root.TryGetProperty("title", out JsonElement title) &&
            title.ValueKind == JsonValueKind.Object &&
            title.TryGetProperty("itemId", out JsonElement titleId) &&
            titleId.ToString() is { Length: > 0 } titleValue &&
            titleValue != "-1")
        {
            payload["title"] = titleValue;
        }

        LeagueClientApiResponse response = await api.SendAsync(
            HttpMethod.Post,
            "/lol-challenges/v1/update-player-preferences/",
            JsonSerializer.Serialize(payload),
            cancellationToken);

        return ToResult(
            response,
            "Profile banner changed to the last-rank banner. You may need to refresh your profile to see it.",
            "League Client rejected the banner update.");
    }

    private async Task<LeagueClientOperationResult> SendAsync(
        HttpMethod method,
        string endpoint,
        object payload,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var connection = await detector.DetectAsync(cancellationToken);
        if (connection is null)
        {
            return NotConnected();
        }

        using ILeagueClientApiClient api = apiClientFactory.Create(connection);
        LeagueClientApiResponse response = await api.SendAsync(
            method,
            endpoint,
            JsonSerializer.Serialize(payload),
            cancellationToken);

        return ToResult(response, successMessage, "League Client rejected the request.");
    }

    private static LeagueClientOperationResult ToResult(
        LeagueClientApiResponse response,
        string successMessage,
        string failurePrefix)
    {
        if (response.IsSuccessStatusCode)
        {
            return new LeagueClientOperationResult(true, successMessage, response.StatusCode, response.Body);
        }

        return new LeagueClientOperationResult(
            false,
            $"{failurePrefix} Status: {response.StatusCode}.",
            response.StatusCode,
            response.Body);
    }

    private static LeagueClientOperationResult NotConnected() =>
        new(false, "League Client is not running, or the lockfile could not be read.");

    private static bool TryReadInt32(JsonElement value, out int result)
    {
        result = default;

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetInt32(out result);
        }

        return value.ValueKind == JsonValueKind.String &&
               int.TryParse(value.GetString(), out result);
    }
}
