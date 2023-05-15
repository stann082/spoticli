using SpotifyAPI.Web;

namespace core.services;

public interface ISpotifyService
{

    // attributes
    SpotifyClientConfig Config { get; }
    SpotifyClient Spotify { get; }
    OAuthClient OAuth { get; }

    // behavior
    bool EnsureUserLoggedIn(out SpotifyClient spotify);
    bool EnsureCredentialsSet(out SpotifyClient spotify);

}
