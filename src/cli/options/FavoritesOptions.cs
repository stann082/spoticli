using CommandLine;

namespace cli.options;

[Verb("favorites", HelpText = "Gets information and manage spotify favorites list.")]
public class FavoritesOptions
{
    [Option('d', "delete", HelpText = "Deletes all items from favorites.")]
    public bool Delete { get; set; }

    [Option("remove-from-playlists", HelpText = "Finds favorite tracks across playlists and removes them.")]
    public bool RemoveFromPlaylists { get; set; }
}
