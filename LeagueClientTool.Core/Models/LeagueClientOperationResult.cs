namespace LeagueClientTool.Core.Models;

public sealed record LeagueClientOperationResult(
    bool Success,
    string Message,
    int? StatusCode = null,
    string? ResponseBody = null);
