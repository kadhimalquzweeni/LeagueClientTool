using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Restarts the League client interface by closing its UX and renderer processes.
/// This does not terminate the League game process.
/// </summary>
public sealed class ClientToolsService(ILeagueClientUxProcessManager processManager) : IClientToolsService
{
    private static readonly string[] ProcessNames = ["LeagueClientUx", "LeagueClientUxRender"];

    public LeagueClientOperationResult RestartLeagueUx()
    {
        int killedCount = 0;
        var errors = new List<string>();

        foreach (string processName in ProcessNames)
        {
            IReadOnlyList<int> processIds;

            try
            {
                processIds = processManager.FindProcessIds(processName);
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                errors.Add($"{processName}: {exception.Message}");
                continue;
            }

            foreach (int processId in processIds)
            {
                try
                {
                    processManager.Kill(processId);
                    killedCount++;
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException or
                    System.ComponentModel.Win32Exception or
                    ArgumentException)
                {
                    errors.Add($"{processName} ({processId}): {exception.Message}");
                }
            }
        }

        if (killedCount == 0 && errors.Count == 0)
        {
            return new LeagueClientOperationResult(
                false,
                "League UX processes were not found. Make sure the League Client is open.");
        }

        if (errors.Count > 0)
        {
            return new LeagueClientOperationResult(
                false,
                $"Restart attempted and closed {killedCount} process(es), but some could not be closed: {string.Join(" | ", errors)}");
        }

        return new LeagueClientOperationResult(
            true,
            $"Restarted League UX. Closed {killedCount} process(es). The League interface should reopen shortly.");
    }
}
