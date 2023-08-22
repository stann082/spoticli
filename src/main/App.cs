using cli.commands;
using cli.options;
using CommandLine;
using core.config;
using core.services;

namespace main;

public class App
{

    #region Constructors

    public App(ApplicationConfig config, ILoginService loginService, ISpotifyService spotifyService)
    {
        _config = config;
        _loginService = loginService;
        _spotifyService = spotifyService;
    }

    #endregion

    #region Variables

    private readonly ApplicationConfig _config;
    private readonly ILoginService _loginService;
    private readonly ISpotifyService _spotifyService;

    #endregion

    #region Public Methods

    public int RunApp(IEnumerable<string> args)
    {
        return Parser.Default.ParseArguments<ConfigOptions,
                FavoritesOptions,
                LoginOptions,
                LogoutOptions,
                PlaylistsOptions,
                TracksOptions>(args)
            .MapResult(
                (ConfigOptions opts) => ConfigCommand.Execute(opts, _config),
                (FavoritesOptions opts) => FavoritesCommand.Execute(opts, _spotifyService),
                (LoginOptions opts) => LoginCommand.Execute(opts, _config, _loginService),
                (LogoutOptions _) => LogoutCommand.Execute(_config),
                (PlaylistsOptions opts) => PlaylistsCommand.Execute(opts, _spotifyService),
                (TracksOptions opts) => TracksCommand.Execute(opts, _spotifyService),
                errs => 1);
    }

    #endregion

}
