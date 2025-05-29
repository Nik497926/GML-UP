using System.Text.Json.Serialization;
using Pingo.Converters;

namespace Pingo.Networking.Java.Protocol.Components;

public sealed class ChatMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("bold")]
    public bool Bold { get; set; }

    [JsonPropertyName("italic")]
    public bool Italic { get; set; }

    [JsonPropertyName("underlined")]
    public bool Underlined { get; set; }

    [JsonPropertyName("strikeThrough")]
    public bool StrikeThrough { get; set; }

    [JsonPropertyName("obfuscated")]
    public bool Obfuscated { get; set; }

    [JsonConverter(typeof(ColorConverter))]
    [JsonPropertyName("color")]
    public Color Color { get; set; } = Color.White;

    // Это свойство будет десериализовать вложенные объекты
    [JsonPropertyName("extra")]
    public ChatMessage[]? Extra { get; set; }

    public static ChatMessage FromString(string text)
    {
        return new ChatMessage { Text = text };
    }
}

public enum Color
{
    Black,
    DarkBlue,
    DarkGreen,
    DarkAqua,
    DarkRed,
    DarkPurple,
    Gold,
    Gray,
    DarkGray,
    Blue,
    Green,
    Aqua,
    Red,
    LightPurple,
    Yellow,
    White
}
