using System.Text;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class SystemLockfileReaderTests
{
    [Fact]
    public async Task ReadAllTextAsync_ReadsLockfileWhileLeagueKeepsItOpen()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"league-lockfile-{Guid.NewGuid():N}");

        const string expected = "LeagueClient:1234:54321:example-password:https";

        try
        {
            await using (var clientLockfile = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                var bytes = Encoding.UTF8.GetBytes(expected);
                await clientLockfile.WriteAsync(bytes);
                await clientLockfile.FlushAsync();

                var reader = new SystemLockfileReader();

                var actual = await reader.ReadAllTextAsync(path);

                Assert.Equal(expected, actual);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_PropagatesCancellation()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"league-lockfile-{Guid.NewGuid():N}");

        await File.WriteAllTextAsync(path, "LeagueClient:1234:54321:password:https");

        try
        {
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            var reader = new SystemLockfileReader();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => reader.ReadAllTextAsync(path, cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
