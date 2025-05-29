using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pingo.Networking.Java.Protocol.Components;

namespace Pingo.Converters;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var color = reader.GetString();
        if (color == null) return Color.White;

        return Enum.TryParse<Color>(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(color), out var resultColor)
            ? resultColor
            : Color.White;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLower());
    }
}
