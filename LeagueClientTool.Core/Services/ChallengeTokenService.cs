using System.Text.Json;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Provides the challenge-token actions from the previous MVC application.
/// </summary>
public sealed class ChallengeTokenService(
    ILeagueClientDetector detector,
    ILeagueClientApiClientFactory apiClientFactory) : IChallengeTokenService
{
    private const string PreferencesEndpoint = "/lol-challenges/v1/update-player-preferences/";
    private const string ClearPreferencesEndpoint = "/lol-challenges/v1/update-player-preferences";
    private const string SummaryEndpoint = "/lol-challenges/v1/summary-player-data/local-player";

    public async Task<LeagueClientOperationResult> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        return await SendPreferencesAsync(
            new { challengeIds = Array.Empty<int>() },
            ClearPreferencesEndpoint,
            "Challenge tokens cleared successfully. You may need to refresh or restart the League Client to see the change.",
            cancellationToken);
    }

    public async Task<LeagueClientOperationResult> CopyFirstTokenToAllSlotsAsync(
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
            summaryJson = await api.GetAsync(SummaryEndpoint, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return new LeagueClientOperationResult(
                false,
                "Could not read challenge data from the League Client.",
                (int?)exception.StatusCode,
                exception.Message);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(summaryJson);
        }
        catch (JsonException exception)
        {
            return new LeagueClientOperationResult(
                false,
                "The League Client returned invalid challenge data.",
                ResponseBody: exception.Message);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("topChallenges", out JsonElement topChallenges) ||
                topChallenges.ValueKind != JsonValueKind.Array ||
                topChallenges.GetArrayLength() == 0 ||
                !topChallenges[0].TryGetProperty("id", out JsonElement firstId) ||
                !TryReadInt32(firstId, out int tokenId))
            {
                return new LeagueClientOperationResult(
                    false,
                    "No equipped challenge token with a valid ID was found. Equip a token in the League Client first.");
            }

            var payload = new Dictionary<string, object?>
            {
                ["challengeIds"] = new[] { tokenId, tokenId, tokenId }
            };

            if (root.TryGetProperty("title", out JsonElement title) &&
                title.ValueKind == JsonValueKind.Object &&
                title.TryGetProperty("itemId", out JsonElement titleId) &&
                titleId.ToString() is { Length: > 0 } titleValue &&
                titleValue != "-1")
            {
                payload["title"] = titleValue;
            }

            if (root.TryGetProperty("bannerId", out JsonElement bannerId) &&
                bannerId.ValueKind is JsonValueKind.Number or JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(bannerId.ToString()))
            {
                payload["bannerAccent"] = bannerId.ToString();
            }

            LeagueClientApiResponse response = await api.SendAsync(
                HttpMethod.Post,
                PreferencesEndpoint,
                JsonSerializer.Serialize(payload),
                cancellationToken);

            return ToResult(
                response,
                $"Copied challenge token {tokenId} into all 3 slots.",
                "League Client rejected the challenge-token update.");
        }
    }

    private async Task<LeagueClientOperationResult> SendPreferencesAsync(
        object payload,
        string endpoint,
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
            HttpMethod.Post,
            endpoint,
            JsonSerializer.Serialize(payload),
            cancellationToken);

        return ToResult(response, successMessage, "League Client rejected the challenge-token update.");
    }

    private static LeagueClientOperationResult ToResult(
        LeagueClientApiResponse response,
        string successMessage,
        string failureMessage) =>
        response.IsSuccessStatusCode
            ? new LeagueClientOperationResult(true, successMessage, response.StatusCode, response.Body)
            : new LeagueClientOperationResult(
                false,
                $"{failureMessage} Status: {response.StatusCode}.",
                response.StatusCode,
                response.Body);

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
