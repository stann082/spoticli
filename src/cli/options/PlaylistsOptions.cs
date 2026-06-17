using CommandLine;
using core;

namespace cli.options;

[Verb("playlists", HelpText = "Gets information and manage spotify playlists.")]
public class PlaylistsOptions : AbstractOption, IPlaylistsOptions
{
    
    [Option('c', "create", HelpText = "Create new playlist. Optionally provide a file path with track IDs via --track-ids-file.")]
    public string NewPlaylist { get; set; }

    [Option("track-ids-file", HelpText = "Path to a file containing track IDs (one per line) to add when creating a playlist.")]
    public string TrackIdsFile { get; set; }
    
    [Option("find-duplicates", HelpText = "Searches for duplicate items in playlists.")]
    public bool ShouldFindDuplicates { get; set; }
    
    [Option('q', "query", HelpText = "Search for a specific playlist.")]
    public string Query { get; set; }
    
    [Option('t', "tracks", HelpText = "List out tracks for a playlist.")]
    public bool Tracks { get; set; }
    
    [Option("show-playlist-id", HelpText = "Displays playlist id.")]
    public bool ShowPlaylistId { get; set; }
    
    [Option("show-track-id", HelpText = "Displays track id.")]
    public bool ShowTrackId { get; set; }

    [Option("dj-mix", HelpText = "Clone playlist reordered for smooth DJ transitions by tempo and key.")]
    public bool DjMix { get; set; }

}
