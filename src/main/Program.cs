using core.config;
using core.services;
using Microsoft.Extensions.DependencyInjection;

namespace main;

public static class Program
{

    #region Main Method

    public static int Main(string[] args)
    {
        var config = ApplicationConfig.Load();
        var services = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<App>()
            .AddSingleton<ISpotifyService, SpotifyService>()
            .AddSingleton<ILoginService, LoginService>()
            .BuildServiceProvider();

        return services.GetService<App>().RunApp(args);
    }

    #endregion

}
