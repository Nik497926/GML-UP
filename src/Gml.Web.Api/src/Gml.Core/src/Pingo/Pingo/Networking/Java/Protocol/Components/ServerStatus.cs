using System.Text.Json.Serialization;
using Pingo.Converters;

namespace Pingo.Networking.Java.Protocol.Components;

internal sealed class ServerStatus
{
    public ServerVersion Version { get; set; }

    [JsonPropertyName("players")]
    public PlayerInformation PlayerInformation { get; set; }

    [JsonConverter(typeof(DescriptionConverter))]
    public Description? Description { get; set; }

    public string Favicon { get; set; } = string.Empty;
}

public class Description
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("extra")] public ChatMessage[] Extra { get; set; } = [];
}

internal sealed class ServerVersion
{
    public string Name { get; set; }

    public int Protocol { get; set; }
}

internal sealed class PlayerInformation
{
    public int Max { get; set; }

    public int Online { get; set; }
}
