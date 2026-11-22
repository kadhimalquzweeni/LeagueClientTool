using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Locates the LeagueClientUx executable and reads the lockfile beside it.
/// </summary>
public sealed class LeagueClientDetector(
    ILeagueClientProcessLocator processLocator,
    ILockfileReader lockfileReader) : ILeagueClientDetector
{
    public async Task<LeagueClientConnectionInfo?> DetectAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processes = processLocator.FindRunningClients();

        foreach (var process in processes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var executableDirectory = Path.GetDirectoryName(process.ExecutablePath);

            if (string.IsNullOrWhiteSpace(executableDirectory))
            {
                continue;
            }

            var lockfilePath = Path.Combine(executableDirectory, "lockfile");

            try
            {
                var contents = await lockfileReader.ReadAllTextAsync(
                    lockfilePath,
                    cancellationToken);

                if (LeagueClientLockfileParser.TryParse(contents, out var connectionInfo))
                {
                    return connectionInfo;
                }
            }
            catch (FileNotFoundException)
            {
                // The client may be starting or closing; check any other instances.
            }
            catch (DirectoryNotFoundException)
            {
                // The installation directory may have been removed during shutdown.
            }
            catch (UnauthorizedAccessException)
            {
                // Try other detected client instances if this installation is inaccessible.
            }
            catch (IOException)
            {
                // The lockfile can briefly be unavailable while the client updates it.
            }
        }

        return null;
    }
}
