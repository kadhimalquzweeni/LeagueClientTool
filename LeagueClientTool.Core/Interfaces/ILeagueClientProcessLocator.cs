namespace LeagueClientTool.Core.Interfaces;

public sealed record LeagueClientProcess(int ProcessId, string ExecutablePath);

public interface ILeagueClientProcessLocator
{
    IReadOnlyList<LeagueClientProcess> FindRunningClients();
}
