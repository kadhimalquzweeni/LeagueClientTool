using System.Globalization;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Parses the five colon-separated fields written by the League client lockfile.
/// </summary>
public static class LeagueClientLockfileParser
{
    public static bool TryParse(
        string? contents,
        out LeagueClientConnectionInfo? connectionInfo)
    {
        connectionInfo = null;

        if (string.IsNullOrWhiteSpace(contents))
        {
            return false;
        }

        var fields = contents.Trim().Split(':');

        if (fields.Length != 5 ||
            !string.Equals(fields[0], "LeagueClient", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var processId) ||
            processId <= 0)
        {
            return false;
        }

        if (!int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
            port is < 1 or > 65535)
        {
            return false;
        }

        var password = fields[3].Trim();
        var protocol = fields[4].Trim();

        if (password.Length == 0 ||
            !string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        connectionInfo = new LeagueClientConnectionInfo(
            processId,
            port,
            password,
            protocol.ToLowerInvariant());

        return true;
    }
}
