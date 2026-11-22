using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using LeagueClientTool.Core.Models;

namespace LeagueClientTool.Core.Services;

/// <summary>
/// Creates an authenticated HTTP client for the local League Client API.
/// The League client uses a self-signed certificate, so certificate validation is
/// bypassed only for loopback requests.
/// </summary>
public static class LeagueClientHttpClientFactory
{
    public static HttpClient Create(LeagueClientConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                request?.RequestUri is { IsLoopback: true } && errors != SslPolicyErrors.None
                    ? true
                    : errors == SslPolicyErrors.None
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = connectionInfo.BaseAddress,
            Timeout = TimeSpan.FromSeconds(10)
        };

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"riot:{connectionInfo.Password}"));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
