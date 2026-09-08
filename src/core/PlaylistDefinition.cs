namespace core;

public class PlaylistDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<TrackDefinition> Tracks { get; set; }
}

public class TrackDefinition
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Status { get; set; }
}
