using LeagueClientTool.Core.Interfaces;

namespace LeagueClientTool.Core.Services;

public sealed class SystemLockfileReader : ILockfileReader
{
    public Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);
}
