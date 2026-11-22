using System.ComponentModel;
using System.Diagnostics;
using LeagueClientTool.Core.Interfaces;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Finds running League client UX processes without allowing inaccessible process
/// metadata to prevent detection of other client instances.
/// </summary>
public sealed class SystemLeagueClientProcessLocator : ILeagueClientProcessLocator
{
    public IReadOnlyList<LeagueClientProcess> FindRunningClients()
    {
        var clients = new List<LeagueClientProcess>();

        foreach (var process in Process.GetProcessesByName("LeagueClientUx"))
        {
            using (process)
            {
                try
                {
                    var executablePath = process.MainModule?.FileName;

                    if (!string.IsNullOrWhiteSpace(executablePath))
                    {
                        clients.Add(new LeagueClientProcess(process.Id, executablePath));
                    }
                }
                catch (Exception exception) when (
                    exception is Win32Exception or InvalidOperationException or
                    NotSupportedException or System.Security.SecurityException)
                {
                    // Another process can exit or deny access while its details are read.
                }
            }
        }

        return clients;
    }
}
