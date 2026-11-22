using System.Diagnostics;
using LeagueClientTool.Core.Interfaces;

namespace LeagueClientTool.Core.Services;

public sealed class SystemLeagueClientUxProcessManager : ILeagueClientUxProcessManager
{
    public IReadOnlyList<int> FindProcessIds(string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);

        return Process.GetProcessesByName(processName)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToArray();
    }

    public void Kill(int processId)
    {
        using Process process = Process.GetProcessById(processId);
        process.Kill();
    }
}
