using LeagueClientTool.Core.Services;

namespace LeagueClientTool.Tests.Services;

public sealed class LeagueClientLockfileParserTests
{
    [Fact]
    public void TryParse_ValidLockfile_ReturnsConnectionInformation()
    {
        const string lockfile = "LeagueClient:1234:54321:example-password:https";

        var result = LeagueClientLockfileParser.TryParse(lockfile, out var connectionInfo);

        Assert.True(result);
        Assert.NotNull(connectionInfo);
        Assert.Equal(1234, connectionInfo.ProcessId);
        Assert.Equal(54321, connectionInfo.Port);
        Assert.Equal("example-password", connectionInfo.Password);
        Assert.Equal("https", connectionInfo.Protocol);
        Assert.Equal("https://127.0.0.1:54321/", connectionInfo.BaseAddress.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NullOrWhitespace_ReturnsFalse(string? contents)
    {
        var result = LeagueClientLockfileParser.TryParse(contents, out var connectionInfo);

        Assert.False(result);
        Assert.Null(connectionInfo);
    }

    [Theory]
    [InlineData("LeagueClient:1234:54321:password")]
    [InlineData("LeagueClient:1234:54321:password:https:extra")]
    [InlineData("OtherProcess:1234:54321:password:https")]
    [InlineData("LeagueClient:0:54321:password:https")]
    [InlineData("LeagueClient:-1:54321:password:https")]
    [InlineData("LeagueClient:not-a-pid:54321:password:https")]
    [InlineData("LeagueClient:1234:0:password:https")]
    [InlineData("LeagueClient:1234:65536:password:https")]
    [InlineData("LeagueClient:1234:not-a-port:password:https")]
    [InlineData("LeagueClient:1234:54321::https")]
    [InlineData("LeagueClient:1234:54321:password:http")]
    public void TryParse_MalformedLockfile_ReturnsFalse(string contents)
    {
        var result = LeagueClientLockfileParser.TryParse(contents, out var connectionInfo);

        Assert.False(result);
        Assert.Null(connectionInfo);
    }

    [Fact]
    public void TryParse_NormalisesProcessNameAndProtocolCase()
    {
        const string lockfile = "leagueclient:1234:54321:password:HTTPS";

        var result = LeagueClientLockfileParser.TryParse(lockfile, out var connectionInfo);

        Assert.True(result);
        Assert.NotNull(connectionInfo);
        Assert.Equal("https", connectionInfo.Protocol);
    }

    [Fact]
    public void ConnectionInfo_ToString_DoesNotExposePassword()
    {
        const string secret = "do-not-log-this-password";
        LeagueClientLockfileParser.TryParse(
            $"LeagueClient:1234:54321:{secret}:https",
            out var connectionInfo);

        Assert.NotNull(connectionInfo);
        Assert.DoesNotContain(secret, connectionInfo.ToString());
    }
}
