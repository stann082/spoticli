using CommandLine;

namespace cli.options;

[Verb("favorites", HelpText = "Gets information and manage spotify favorites list.")]
public class FavoritesOptions
{
    [Option("remove-from-playlists", HelpText = "Finds favorite tracks across playlists and removes them.")]
    public bool RemoveFromPlaylists { get; set; }
}
