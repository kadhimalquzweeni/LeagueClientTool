namespace LeagueClientTool.Core.Models;

/// <summary>
/// Represents the HTTP result returned by a League Client API request.
/// </summary>
public sealed record LeagueClientApiResponse(
    int StatusCode,
    string Body)
{
    public bool IsSuccessStatusCode => StatusCode is >= 200 and <= 299;
}
