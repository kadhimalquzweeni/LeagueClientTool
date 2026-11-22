using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

public sealed class LeagueClientApiClientFactory : ILeagueClientApiClientFactory
{
    public ILeagueClientApiClient Create(LeagueClientConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);

        return new LeagueClientApiClient(
            LeagueClientHttpClientFactory.Create(connectionInfo));
    }
}
