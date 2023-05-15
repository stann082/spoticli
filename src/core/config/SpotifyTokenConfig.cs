﻿namespace core.config;

public class SpotifyTokenConfig
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int? ExpiresIn { get; set; } = default!;
    public string TokenType { get; set; } = default!;
    public DateTime? CreatedAt { get; set; } = default!;
}
