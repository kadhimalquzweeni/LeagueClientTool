using System.Net.Http;
using System.Windows;
using LeagueClientTool.App.ViewModels;
using LeagueClientTool.Core.Interfaces;
using LeagueClientTool.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueClientTool.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<ILeagueClientProcessLocator, SystemLeagueClientProcessLocator>();
        services.AddSingleton<ILockfileReader, SystemLockfileReader>();
        services.AddSingleton<ILeagueClientDetector, LeagueClientDetector>();
        services.AddSingleton<ILeagueClientApiClientFactory, LeagueClientApiClientFactory>();
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
        services.AddSingleton<IDataDragonService, DataDragonService>();
        services.AddSingleton<ILeagueProfileService, LeagueProfileService>();
        services.AddSingleton<ILeagueCustomRequestService, LeagueCustomRequestService>();
        services.AddSingleton<IChallengeTokenService, ChallengeTokenService>();
        services.AddSingleton<IFriendToolsService, FriendToolsService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
