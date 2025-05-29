namespace Pingo.Status;

/// <summary>
/// Represents a Minecraft: Bedrock edition server status.
/// </summary>
public sealed class BedrockStatus : StatusBase
{
    /// <summary>
    /// Stores the game edition of the server, for example Minecraft: Education edition.
    /// </summary>
    public string Edition { get; set; } = null!;

    /// <summary>
    /// Stores possibly multiple lines of MOTDs.
    /// </summary>
    public string[] MessagesOfTheDay { get; set; } = null!;

    /// <summary>
    /// Stores the protocol version of the server.
    /// </summary>
    public int Protocol { get; set; }

    /// <summary>
    /// Stores the game version that is supported by the server.
    /// </summary>
    public string Version { get; set; } = null!;

    /// <summary>
    /// Stores the online amount of players.
    /// </summary>
    public int OnlinePlayers { get; set; }

    /// <summary>
    /// Stores the maximum amount of players.
    /// </summary>
    public int MaximumPlayers { get; set; }

    /// <summary>
    /// Stores the server's unique identifier.
    /// </summary>
    public long ServerIdentifier { get; set; }

    /// <summary>
    /// Stores the server's game mode.
    /// </summary>
    public string GameMode { get; set; } = null!;
}
