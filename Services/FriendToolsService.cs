using LoLClientTool.Models;
using LoLClientTool.Mvc.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LoLClientTool.Services
{
    public class FriendToolsService : IFriendToolsService
    {
        private readonly ILeagueClientDetector _leagueClientDetector;

        public FriendToolsService(ILeagueClientDetector leagueClientDetector)
        {
            _leagueClientDetector = leagueClientDetector;
        }

        public async Task<LeagueClientResult> AcceptAllFriendRequestsAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return NotConnectedResult();
            }

            try
            {
                using HttpClient httpClient = CreateHttpClient(connection);

                List<JsonElement> friendRequests =
                    await GetFriendRequestsAsync(httpClient, connection);

                friendRequests = friendRequests
                    .Where(IsIncomingRequest)
                    .ToList();

                if (!friendRequests.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "No incoming friend requests were found."
                    };
                }

                int acceptedCount = 0;
                List<string> errors = new();

                foreach (JsonElement request in friendRequests)
                {
                    string? requestId = GetRequestId(request);

                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        continue;
                    }

                    string url =
                        $"{GetBaseUrl(connection)}/lol-chat/v2/friend-requests/{Uri.EscapeDataString(requestId)}";

                    using var content = BuildFriendRequestContent(request, requestId);

                    HttpResponseMessage response =
                        await httpClient.PutAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        acceptedCount++;
                    }
                    else
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        errors.Add($"{requestId}: {(int)response.StatusCode} {responseBody}");
                    }
                }


                if (errors.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Accepted {acceptedCount} request(s), but some failed: {string.Join(" | ", errors)}"
                    };
                }

                return new LeagueClientResult
                {
                    Success = true,
                    Message = $"Accepted {acceptedCount} friend request(s)."
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to accept friend requests: {ex.Message}"
                };
            }
        }

        public async Task<LeagueClientResult> DeleteAllFriendRequestsAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return NotConnectedResult();
            }

            try
            {
                using HttpClient httpClient = CreateHttpClient(connection);

                List<JsonElement> friendRequests =
    await GetFriendRequestsAsync(httpClient, connection);

                if (!friendRequests.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "No pending friend requests were found."
                    };
                }

                int deletedCount = 0;
                List<string> errors = new();

                foreach (JsonElement request in friendRequests)
                {
                    string? requestId = GetRequestId(request);

                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        continue;
                    }

                    string url =
                        $"{GetBaseUrl(connection)}/lol-chat/v2/friend-requests/{Uri.EscapeDataString(requestId)}";

                    HttpResponseMessage response =
                        await httpClient.DeleteAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        deletedCount++;
                    }
                    else
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        errors.Add($"{requestId}: {(int)response.StatusCode} {responseBody}");
                    }
                }

                if (errors.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Deleted {deletedCount} request(s), but some failed: {string.Join(" | ", errors)}"
                    };
                }

                return new LeagueClientResult
                {
                    Success = true,
                    Message = $"Deleted {deletedCount} friend request(s)."
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to delete friend requests: {ex.Message}"
                };
            }
        }

        private static async Task<List<JsonElement>> GetFriendRequestsAsync(
    HttpClient httpClient,
    LeagueClientConnection connection)
        {
            string url =
                $"{GetBaseUrl(connection)}/lol-chat/v2/friend-requests";

            HttpResponseMessage response = await httpClient.GetAsync(url);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Friend request endpoint failed. Status: {(int)response.StatusCode}. Response: {json}");
            }

            List<JsonElement>? requests =
                JsonSerializer.Deserialize<List<JsonElement>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return requests ?? new List<JsonElement>();
        }

        private static HttpClient CreateHttpClient(LeagueClientConnection connection)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = new HttpClient(handler);

            string credentials = $"riot:{connection.Password}";
            string encodedCredentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes(credentials));

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", encodedCredentials);

            return httpClient;
        }

        private static string GetBaseUrl(LeagueClientConnection connection)
        {
            return $"{connection.Protocol}://127.0.0.1:{connection.Port}";
        }

        private static LeagueClientResult NotConnectedResult()
        {
            return new LeagueClientResult
            {
                Success = false,
                Message = "League Client is not running, or the lockfile could not be read."
            };
        }

        private class FriendRequest
        {
            [JsonPropertyName("id")]
            public JsonElement Id { get; set; }

            public string? GetRequestId()
            {
                if (Id.ValueKind == JsonValueKind.Number)
                {
                    return Id.GetInt64().ToString();
                }

                if (Id.ValueKind == JsonValueKind.String)
                {
                    return Id.GetString();
                }

                return null;
            }
        }
        private static string? GetRequestId(JsonElement request)
        {
            string? puuid = GetStringProperty(request, "puuid");

            if (!string.IsNullOrWhiteSpace(puuid) && puuid != "0")
            {
                return puuid;
            }

            string? pid = GetStringProperty(request, "pid");

            if (!string.IsNullOrWhiteSpace(pid) && pid != "0")
            {
                return pid;
            }

            string? id = GetStringProperty(request, "id");

            if (!string.IsNullOrWhiteSpace(id) && id != "0")
            {
                return id;
            }

            return null;
        }

        private static string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }

            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt64().ToString();
            }

            return null;
        }

        private static bool IsIncomingRequest(JsonElement request)
        {
            if (!request.TryGetProperty("direction", out JsonElement directionElement))
            {
                return true;
            }

            string? direction = directionElement.GetString();

            return !string.Equals(direction, "out", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(direction, "outgoing", StringComparison.OrdinalIgnoreCase);
        }
        private static StringContent BuildFriendRequestContent(
    JsonElement request,
    string requestId)
        {
            using JsonDocument document = JsonDocument.Parse(request.GetRawText());

            Dictionary<string, object?> payload = new();

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                payload[property.Name] = JsonSerializer.Deserialize<object>(
                    property.Value.GetRawText());
            }

            payload["id"] = requestId;

            if (!payload.ContainsKey("note"))
            {
                payload["note"] = "";
            }

            string json = JsonSerializer.Serialize(payload);

            return new StringContent(
                json,
                Encoding.UTF8,
                "application/json");
        }
        public async Task<List<FriendGroupOptionViewModel>> GetFriendGroupsAsync()
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return new List<FriendGroupOptionViewModel>();
            }

            try
            {
                using HttpClient httpClient = CreateHttpClient(connection);

                string url =
                    $"{GetBaseUrl(connection)}/lol-chat/v1/friend-groups";

                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new List<FriendGroupOptionViewModel>
            {
                new FriendGroupOptionViewModel
                {
                    GroupId = 0,
                    Name = "General"
                }
            };
                }

                string json = await response.Content.ReadAsStringAsync();

                List<JsonElement>? groups =
                    JsonSerializer.Deserialize<List<JsonElement>>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                List<FriendGroupOptionViewModel> friendGroups = new()
        {
            new FriendGroupOptionViewModel
            {
                GroupId = 0,
                Name = "General"
            }
        };

                if (groups == null)
                {
                    return friendGroups;
                }

                List<FriendGroupOptionViewModel> customGroups = groups
                    .Select(group =>
                    {
                        int groupId = GetIntProperty(group, "id") ?? -1;
                        string groupName = GetStringProperty(group, "name") ?? $"Group {groupId}";

                        return new FriendGroupOptionViewModel
                        {
                            GroupId = groupId,
                            Name = groupName
                        };
                    })
                    .Where(group => group.GroupId > 0)
                    .OrderBy(group => group.Name)
                    .ToList();

                friendGroups.AddRange(customGroups);

                return friendGroups;
            }
            catch
            {
                return new List<FriendGroupOptionViewModel>
        {
            new FriendGroupOptionViewModel
            {
                GroupId = 0,
                Name = "General"
            }
        };
            }
        }
        public async Task<LeagueClientResult> DeleteFriendsFromGroupAsync(int groupId)
        {
            LeagueClientConnection? connection = _leagueClientDetector.GetConnection();

            if (connection == null)
            {
                return NotConnectedResult();
            }

            try
            {
                using HttpClient httpClient = CreateHttpClient(connection);

                string friendsUrl =
                    $"{GetBaseUrl(connection)}/lol-chat/v1/friends";

                HttpResponseMessage friendsResponse = await httpClient.GetAsync(friendsUrl);

                string friendsJson = await friendsResponse.Content.ReadAsStringAsync();

                if (!friendsResponse.IsSuccessStatusCode)
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Could not read friends. Status: {(int)friendsResponse.StatusCode}. Response: {friendsJson}"
                    };
                }

                List<JsonElement>? friends =
                    JsonSerializer.Deserialize<List<JsonElement>>(
                        friendsJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (friends == null || !friends.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "No friends were found."
                    };
                }

                List<JsonElement> friendsInGroup = friends
                    .Where(friend => GetIntProperty(friend, "groupId") == groupId)
                    .ToList();

                if (!friendsInGroup.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = true,
                        Message = "No friends were found in the selected folder."
                    };
                }

                int deletedCount = 0;
                List<string> errors = new();

                foreach (JsonElement friend in friendsInGroup)
                {
                    string? friendId = GetFriendId(friend);

                    if (string.IsNullOrWhiteSpace(friendId))
                    {
                        errors.Add("Could not find friend ID.");
                        continue;
                    }

                    string deleteUrl =
                        $"{GetBaseUrl(connection)}/lol-chat/v1/friends/{Uri.EscapeDataString(friendId)}";

                    HttpResponseMessage deleteResponse =
                        await httpClient.DeleteAsync(deleteUrl);

                    if (deleteResponse.IsSuccessStatusCode)
                    {
                        deletedCount++;
                    }
                    else
                    {
                        string responseBody = await deleteResponse.Content.ReadAsStringAsync();
                        errors.Add($"{friendId}: {(int)deleteResponse.StatusCode} {responseBody}");
                    }
                }

                if (errors.Any())
                {
                    return new LeagueClientResult
                    {
                        Success = false,
                        Message = $"Deleted {deletedCount} friend(s), but some failed: {string.Join(" | ", errors)}"
                    };
                }

                return new LeagueClientResult
                {
                    Success = true,
                    Message = $"Deleted {deletedCount} friend(s) from the selected folder."
                };
            }
            catch (Exception ex)
            {
                return new LeagueClientResult
                {
                    Success = false,
                    Message = $"Failed to delete friends from folder: {ex.Message}"
                };
            }
        }
        private static int? GetIntProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out int number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String
                && int.TryParse(property.GetString(), out int parsedNumber))
            {
                return parsedNumber;
            }

            return null;
        }

        private static string? GetFriendId(JsonElement friend)
        {
            string? id = GetStringProperty(friend, "id");

            if (!string.IsNullOrWhiteSpace(id) && id != "0")
            {
                return id;
            }

            string? puuid = GetStringProperty(friend, "puuid");

            if (!string.IsNullOrWhiteSpace(puuid) && puuid != "0")
            {
                return puuid;
            }

            string? pid = GetStringProperty(friend, "pid");

            if (!string.IsNullOrWhiteSpace(pid) && pid != "0")
            {
                return pid;
            }

            return null;
        }
    }
}