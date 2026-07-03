using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoLClientTool.Services
{
    public class LeagueClientService : ILeagueClientService
    {
        private readonly ILeagueClientDetector _leagueClientDetector;

        public LeagueClientService(ILeagueClientDetector leagueClientDetector)
        {
            _leagueClientDetector = leagueClientDetector;
        }

        public async Task<LeagueClientResult> SetProfileIconAsync(int iconId)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-summoner/v1/current-summoner/icon";

                var payload = new
                {
                    profileIconId = iconId
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = $"Profile icon changed to {iconId}."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to update profile icon: {ex.Message}"
                };
            }
        }

        public async Task<LeagueClientResult> SetProfileBackgroundAsync(int skinId)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-summoner/v1/current-summoner/summoner-profile";

                var payload = new
                {
                    key = "backgroundSkinId",
                    value = skinId
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = $"Profile background changed to skin ID {skinId}."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to update profile background: {ex.Message}"
                };
            }
        }
        public async Task<LeagueClientResult> UpdateStatusMessageAsync(string statusMessage)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-chat/v1/me";

                var payload = new
                {
                    statusMessage = statusMessage
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "Profile status message updated successfully."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to update status message: {ex.Message}"
                };
            }
        }
        public async Task<LeagueClientResult> ClearChallengeTokensAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-challenges/v1/update-player-preferences";

                var payload = new
                {
                    challengeIds = Array.Empty<int>()
                };

                string json = System.Text.Json.JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "Challenge tokens cleared successfully. You may need to refresh or restart the League Client to see the change."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to clear challenge tokens: {ex.Message}"
                };
            }
        }
        public async Task<LeagueClientResult> CopyFirstChallengeTokenToAllSlotsAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string baseUrl = $"{connection.Protocol}://127.0.0.1:{connection.Port}";

                string summaryUrl =
                    $"{baseUrl}/lol-challenges/v1/summary-player-data/local-player";

                HttpResponseMessage summaryResponse = await httpClient.GetAsync(summaryUrl);

                if (!summaryResponse.IsSuccessStatusCode)
                {
                    string responseBody = await summaryResponse.Content.ReadAsStringAsync();

                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Could not read challenge data. Status: {(int)summaryResponse.StatusCode}. Response: {responseBody}"
                    };
                }

                string summaryJson = await summaryResponse.Content.ReadAsStringAsync();

                using JsonDocument document = JsonDocument.Parse(summaryJson);

                JsonElement root = document.RootElement;

                if (!root.TryGetProperty("topChallenges", out JsonElement topChallenges)
                    || topChallenges.ValueKind != JsonValueKind.Array
                    || topChallenges.GetArrayLength() == 0)
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = "No equipped challenge token was found. Equip a token in the League Client first, save it, close the identity customisation window, then try again."
                    };
                }

                JsonElement firstChallenge = topChallenges[0];

                if (!firstChallenge.TryGetProperty("id", out JsonElement firstChallengeIdElement))
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = "The first equipped challenge token did not contain an ID."
                    };
                }

                int firstTokenId;

                if (firstChallengeIdElement.ValueKind == JsonValueKind.Number)
                {
                    firstTokenId = firstChallengeIdElement.GetInt32();
                }
                else if (firstChallengeIdElement.ValueKind == JsonValueKind.String
                         && int.TryParse(firstChallengeIdElement.GetString(), out int parsedId))
                {
                    firstTokenId = parsedId;
                }
                else
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = "Could not parse the first challenge token ID."
                    };
                }

                string? titleId = null;

                if (root.TryGetProperty("title", out JsonElement titleElement)
                    && titleElement.ValueKind == JsonValueKind.Object
                    && titleElement.TryGetProperty("itemId", out JsonElement titleIdElement))
                {
                    titleId = titleIdElement.ToString();
                }

                string? bannerId = null;

                if (root.TryGetProperty("bannerId", out JsonElement bannerElement))
                {
                    bannerId = bannerElement.ToString();
                }

                var payload = new Dictionary<string, object?>
                {
                    ["challengeIds"] = new[]
                    {
                firstTokenId,
                firstTokenId,
                firstTokenId
            }
                };

                if (!string.IsNullOrWhiteSpace(titleId) && titleId != "-1")
                {
                    payload["title"] = titleId;
                }

                if (!string.IsNullOrWhiteSpace(bannerId))
                {
                    payload["bannerAccent"] = bannerId;
                }

                string updateJson = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    updateJson,
                    Encoding.UTF8,
                    "application/json");

                string updateUrl =
                    $"{baseUrl}/lol-challenges/v1/update-player-preferences/";

                HttpResponseMessage updateResponse =
                    await httpClient.PostAsync(updateUrl, content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = $"Copied challenge token {firstTokenId} into all 3 slots."
                    };
                }

                string updateResponseBody = await updateResponse.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the update. Status: {(int)updateResponse.StatusCode}. Response: {updateResponseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to copy challenge token: {ex.Message}"
                };
            }
        }

        public async Task<LeagueClientResult> SetLastRankBannerAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string baseUrl = $"{connection.Protocol}://127.0.0.1:{connection.Port}";

                string summaryUrl =
                    $"{baseUrl}/lol-challenges/v1/summary-player-data/local-player";

                HttpResponseMessage summaryResponse = await httpClient.GetAsync(summaryUrl);

                if (!summaryResponse.IsSuccessStatusCode)
                {
                    string responseBody = await summaryResponse.Content.ReadAsStringAsync();

                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Could not read current profile preferences. Status: {(int)summaryResponse.StatusCode}. Response: {responseBody}"
                    };
                }

                string summaryJson = await summaryResponse.Content.ReadAsStringAsync();

                using JsonDocument document = JsonDocument.Parse(summaryJson);

                JsonElement root = document.RootElement;

                List<int> challengeIds = new();

                if (root.TryGetProperty("topChallenges", out JsonElement topChallenges)
                    && topChallenges.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement challenge in topChallenges.EnumerateArray())
                    {
                        if (challenge.TryGetProperty("id", out JsonElement idElement))
                        {
                            if (idElement.ValueKind == JsonValueKind.Number)
                            {
                                challengeIds.Add(idElement.GetInt32());
                            }
                            else if (idElement.ValueKind == JsonValueKind.String
                                     && int.TryParse(idElement.GetString(), out int parsedId))
                            {
                                challengeIds.Add(parsedId);
                            }
                        }
                    }
                }

                string? titleId = null;

                if (root.TryGetProperty("title", out JsonElement titleElement)
                    && titleElement.ValueKind == JsonValueKind.Object
                    && titleElement.TryGetProperty("itemId", out JsonElement titleIdElement))
                {
                    titleId = titleIdElement.ToString();
                }

                var payload = new Dictionary<string, object?>
                {
                    ["bannerAccent"] = "2"
                };

                if (challengeIds.Any())
                {
                    payload["challengeIds"] = challengeIds;
                }

                if (!string.IsNullOrWhiteSpace(titleId) && titleId != "-1")
                {
                    payload["title"] = titleId;
                }

                string updateJson = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    updateJson,
                    Encoding.UTF8,
                    "application/json");

                string updateUrl =
                    $"{baseUrl}/lol-challenges/v1/update-player-preferences/";

                HttpResponseMessage updateResponse =
                    await httpClient.PostAsync(updateUrl, content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "Profile banner changed to the last-rank banner. You may need to refresh your profile to see it."
                    };
                }

                string updateResponseBody = await updateResponse.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the banner update. Status: {(int)updateResponse.StatusCode}. Response: {updateResponseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to update profile banner: {ex.Message}"
                };
            }
        }

        public async Task<LeagueClientResult> SetVisibleRankAsync(
        string queue,
        string tier,
        string division)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-chat/v1/me";

                var payload = new
                {
                    lol = new
                    {
                        rankedLeagueQueue = queue,
                        rankedLeagueTier = tier,
                        rankedLeagueDivision = division
                    }
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = $"Visible rank changed to {tier} {division} for {queue}."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to update visible rank: {ex.Message}"
                };
            }
        }
        public async Task<LeagueClientResult> ClearVisibleRankAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-chat/v1/me";

                var payload = new
                {
                    lol = new
                    {
                        rankedLeagueQueue = "",
                        rankedLeagueTier = "",
                        rankedLeagueDivision = ""
                    }
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PutAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "Visible rank cleared."
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the request. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to clear visible rank: {ex.Message}"
                };
            }
        }
        public async Task<LeagueClientResult> ChangeRiotIdAsync(string gameName, string tagLine)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read."
                };
            }

            if (string.IsNullOrWhiteSpace(gameName))
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "Riot ID name cannot be empty."
                };
            }

            if (string.IsNullOrWhiteSpace(tagLine))
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = "Tagline cannot be empty."
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}/lol-summoner/v1/save-alias";

                var payload = new
                {
                    gameName = gameName.Trim(),
                    tagLine = tagLine.Trim()
                };

                string json = JsonSerializer.Serialize(payload);

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = $"Riot ID change request sent for {gameName.Trim()}#{tagLine.Trim()}."
                    };
                }

                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"League Client rejected the Riot ID change. Status: {(int)response.StatusCode}. Response: {responseBody}"
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to change Riot ID: {ex.Message}"
                };
            }
        }
        public async Task<CustomLeagueClientRequestResult> SendCustomRequestAsync(
    string method,
    string endpoint,
    string? body)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = "League Client is not running, or the lockfile could not be read.",
                    StatusCode = 0
                };
            }

            if (string.IsNullOrWhiteSpace(method))
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = "Request method cannot be empty.",
                    StatusCode = 0
                };
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = "Endpoint cannot be empty.",
                    StatusCode = 0
                };
            }

            endpoint = endpoint.Trim();

            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }

            if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = "Only local LCU paths are allowed. Use /lol-example/v1/example, not a full URL.",
                    StatusCode = 0
                };
            }

            string normalisedMethod = method.Trim().ToUpperInvariant();

            string[] allowedMethods =
            {
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE"
    };

            if (!allowedMethods.Contains(normalisedMethod))
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = "Unsupported method. Use GET, POST, PUT, PATCH, or DELETE.",
                    StatusCode = 0
                };
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var httpClient = new HttpClient(handler);

                string credentials = $"riot:{connection.Password}";
                string encodedCredentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(credentials));

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedCredentials);

                string url =
                    $"{connection.Protocol}://127.0.0.1:{connection.Port}{endpoint}";

                using var request = new HttpRequestMessage(
                    new HttpMethod(normalisedMethod),
                    url);

                bool methodAllowsBody =
                    normalisedMethod == "POST"
                    || normalisedMethod == "PUT"
                    || normalisedMethod == "PATCH";

                if (methodAllowsBody && !string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json");
                }

                HttpResponseMessage response = await httpClient.SendAsync(request);

                string responseBody = await response.Content.ReadAsStringAsync();
                string formattedResponseBody = string.IsNullOrWhiteSpace(responseBody)
                    ? $"No response body returned. HTTP {(int)response.StatusCode} {response.StatusCode}."
                    : FormatJsonResponse(responseBody);

                return new CustomLeagueClientRequestResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = formattedResponseBody,
                    Message = response.IsSuccessStatusCode
                        ? response.StatusCode == System.Net.HttpStatusCode.NoContent
                            ? "Request succeeded. The League Client returned 204 No Content."
                            : "Custom request sent successfully."
                        : $"League Client rejected the request. Status: {(int)response.StatusCode}."
                };
            }
            catch (Exception ex)
            {
                return new CustomLeagueClientRequestResult
                {
                    Success = false,
                    Message = $"Failed to send custom request: {ex.Message}",
                    StatusCode = 0
                };
            }
        }
        private static string FormatJsonResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return string.Empty;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);

                return JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
            }
            catch
            {
                return responseBody;
            }
        }

    }
}