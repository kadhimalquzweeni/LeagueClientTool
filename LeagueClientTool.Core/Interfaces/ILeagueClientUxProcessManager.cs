namespace LeagueClientTool.Core.Interfaces;

/// <summary>
/// Small process boundary that allows League UX restart behaviour to be tested without killing real processes.
/// </summary>
public interface ILeagueClientUxProcessManager
{
    IReadOnlyList<int> FindProcessIds(string processName);
    void Kill(int processId);
}
