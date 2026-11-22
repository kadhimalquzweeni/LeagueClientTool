using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Models;
using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class LeagueClientDetectorTests
{
    [Fact]
    public async Task DetectAsync_NoRunningProcesses_ReturnsNull()
    {
        var detector = CreateDetector([]);

        var result = await detector.DetectAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectAsync_ValidLockfile_ReturnsConnectionInformation()
    {
        var process = new LeagueClientProcess(100, @"C:\Riot Games\League of Legends\Game\LeagueClientUx.exe");
        var reader = new FakeLockfileReader("LeagueClient:100:54321:secret:https");
        var detector = new LeagueClientDetector(new FakeProcessLocator([process]), reader);

        var result = await detector.DetectAsync();

        Assert.NotNull(result);
        Assert.Equal(54321, result.Port);
        Assert.Equal("secret", result.Password);
        Assert.Equal(
            Path.Combine(@"C:\Riot Games\League of Legends\Game", "lockfile"),
            reader.LastPath);
    }

    [Fact]
    public async Task DetectAsync_MissingLockfile_ReturnsNull()
    {
        var process = new LeagueClientProcess(100, @"C:\Riot Games\League of Legends\Game\LeagueClientUx.exe");
        var detector = new LeagueClientDetector(
            new FakeProcessLocator([process]),
            new FakeLockfileReader(exception: new FileNotFoundException()));

        var result = await detector.DetectAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectAsync_InvalidLockfile_ReturnsNull()
    {
        var detector = CreateDetector(
            [new LeagueClientProcess(100, @"C:\League\LeagueClientUx.exe")],
            "not-a-valid-lockfile");

        var result = await detector.DetectAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task DetectAsync_TriesNextInstallationWhenFirstLockfileIsMissing()
    {
        var processes = new FakeProcessLocator(
        [
            new LeagueClientProcess(100, @"C:\First\LeagueClientUx.exe"),
            new LeagueClientProcess(200, @"D:\Second\LeagueClientUx.exe")
        ]);
        var reader = new FakeLockfileReader(
            contentsByPath: new Dictionary<string, string>
            {
                [Path.Combine(@"D:\Second", "lockfile")] = "LeagueClient:200:54322:second-secret:https"
            });

        var detector = new LeagueClientDetector(processes, reader);

        var result = await detector.DetectAsync();

        Assert.NotNull(result);
        Assert.Equal(54322, result.Port);
        Assert.Equal("second-secret", result.Password);
    }

    [Fact]
    public async Task DetectAsync_CancellationIsPropagated()
    {
        var detector = CreateDetector([]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => detector.DetectAsync(cancellation.Token));
    }

    private static LeagueClientDetector CreateDetector(
        IReadOnlyList<LeagueClientProcess> processes,
        string? contents = null)
    {
        return new LeagueClientDetector(
            new FakeProcessLocator(processes),
            new FakeLockfileReader(contents ?? string.Empty));
    }

    private sealed class FakeProcessLocator(
        IReadOnlyList<LeagueClientProcess> processes) : ILeagueClientProcessLocator
    {
        public IReadOnlyList<LeagueClientProcess> FindRunningClients() => processes;
    }

    private sealed class FakeLockfileReader : ILockfileReader
    {
        private readonly string? _contents;
        private readonly Exception? _exception;
        private readonly IReadOnlyDictionary<string, string>? _contentsByPath;

        public FakeLockfileReader(
            string? contents = null,
            Exception? exception = null,
            IReadOnlyDictionary<string, string>? contentsByPath = null)
        {
            _contents = contents;
            _exception = exception;
            _contentsByPath = contentsByPath;
        }

        public string? LastPath { get; private set; }

        public Task<string> ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastPath = path;

            if (_exception is not null)
            {
                return Task.FromException<string>(_exception);
            }

            if (_contentsByPath is not null)
            {
                return _contentsByPath.TryGetValue(path, out var contents)
                    ? Task.FromResult(contents)
                    : Task.FromException<string>(new FileNotFoundException());
            }

            return Task.FromResult(_contents ?? string.Empty);
        }
    }
}
