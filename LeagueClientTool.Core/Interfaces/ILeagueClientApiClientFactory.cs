using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Interfaces;

public interface ILeagueClientApiClientFactory
{
    ILeagueClientApiClient Create(LeagueClientConnectionInfo connectionInfo);
}
