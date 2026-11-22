namespace LeagueClientTool.Core.Models;

/// <summary>
/// Holds the connection details read from the League client's local lockfile.
/// The password is intentionally not included in the string representation.
/// </summary>
public sealed class LeagueClientConnectionInfo
{
    public LeagueClientConnectionInfo(int processId, int port, string password, string protocol)
    {
        ProcessId = processId;
        Port = port;
        Password = password;
        Protocol = protocol;
    }

    public int ProcessId { get; }

    public int Port { get; }

    public string Password { get; }

    public string Protocol { get; }

    public Uri BaseAddress => new($"{Protocol}://127.0.0.1:{Port}");

    public override string ToString() =>
        $"League Client (PID: {ProcessId}, Port: {Port}, Protocol: {Protocol})";
}
