using SpotifyAPI.Web;

namespace core;

public class PlaylistTrack
{

    #region Constructors

    // ReSharper disable once UnusedMember.Global
    public PlaylistTrack()
    {
        // for deserialization
    }

    public PlaylistTrack(FullTrack fullTrack)
    {
        Artist = string.Join(',', fullTrack.Artists.Select(a => a.Name));
        Href = fullTrack.Href;
        Id = fullTrack.Id;
        Name = fullTrack.Name;
        Uri = fullTrack.Uri;
    }

    #endregion

    #region Properties

    public string Artist { get; set; }
    public string Href { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Uri { get; set; }

    #endregion

}
