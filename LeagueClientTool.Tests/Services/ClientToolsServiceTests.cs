using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class ClientToolsServiceTests
{
    [Fact]
    public void RestartLeagueUx_WhenBothProcessesExistKillsBoth()
    {
        var manager = new FakeProcessManager();
        manager.Processes["LeagueClientUx"] = [10, 11];
        manager.Processes["LeagueClientUxRender"] = [12];

        var result = new ClientToolsService(manager).RestartLeagueUx();

        Assert.True(result.Success);
        Assert.Equal(3, manager.KilledProcessIds.Count);
        Assert.Contains(10, manager.KilledProcessIds);
        Assert.Contains(11, manager.KilledProcessIds);
        Assert.Contains(12, manager.KilledProcessIds);
    }

    [Fact]
    public void RestartLeagueUx_WhenNoProcessesExistReturnsFailure()
    {
        var manager = new FakeProcessManager();

        var result = new ClientToolsService(manager).RestartLeagueUx();

        Assert.False(result.Success);
        Assert.Contains("were not found", result.Message);
    }

    [Fact]
    public void RestartLeagueUx_WhenOneKillFailsReportsPartialFailure()
    {
        var manager = new FakeProcessManager();
        manager.Processes["LeagueClientUx"] = [10, 11];
        manager.KillFailures.Add(11);

        var result = new ClientToolsService(manager).RestartLeagueUx();

        Assert.False(result.Success);
        Assert.Contains("closed 1 process", result.Message);
        Assert.Contains("11", result.Message);
    }

    [Fact]
    public void RestartLeagueUx_WhenProcessDiscoveryFailsReportsError()
    {
        var manager = new FakeProcessManager();
        manager.DiscoveryFailures.Add("LeagueClientUx");

        var result = new ClientToolsService(manager).RestartLeagueUx();

        Assert.False(result.Success);
        Assert.Contains("LeagueClientUx", result.Message);
    }

    private sealed class FakeProcessManager : ILeagueClientUxProcessManager
    {
        public Dictionary<string, IReadOnlyList<int>> Processes { get; } = new();
        public HashSet<int> KillFailures { get; } = [];
        public HashSet<string> DiscoveryFailures { get; } = [];
        public List<int> KilledProcessIds { get; } = new();

        public IReadOnlyList<int> FindProcessIds(string processName)
        {
            if (DiscoveryFailures.Contains(processName))
            {
                throw new InvalidOperationException("Process enumeration failed.");
            }

            return Processes.TryGetValue(processName, out var ids) ? ids : [];
        }

        public void Kill(int processId)
        {
            if (KillFailures.Contains(processId))
            {
                throw new InvalidOperationException("Access denied.");
            }

            KilledProcessIds.Add(processId);
        }
    }
}
