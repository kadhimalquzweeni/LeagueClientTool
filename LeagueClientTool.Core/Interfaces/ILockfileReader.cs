namespace LeagueClientTool.Core.Interfaces;

public interface ILockfileReader
{
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
}
