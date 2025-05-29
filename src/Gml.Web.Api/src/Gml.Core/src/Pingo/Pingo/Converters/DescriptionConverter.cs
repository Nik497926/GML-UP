using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pingo.Networking.Java.Protocol.Components;

namespace Pingo.Converters;

public class DescriptionConverter : JsonConverter<Description>
{
    public override Description Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var description = new Description();

        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                description.Text = reader.GetString() ?? string.Empty;
                description.Extra = [];
                break;
            case JsonTokenType.StartObject:
                // Read through the object and get its properties accordingly
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString();

                        reader.Read();

                        switch (propertyName)
                        {
                            case "text":
                                description.Text = reader.GetString();
                                break;
                            case "extra":
                                // Используем пользовательский метод для обработки массива, где могут быть и объекты, и строки
                                description.Extra = DeserializeExtraArray(ref reader, options);
                                break;
                        }
                    }

                    // If it's the end of the object, then break out of the loop
                    else if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                }

                break;
        }

        return description;
    }

    private ChatMessage[] DeserializeExtraArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        // Убедимся, что мы находимся в начале массива
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array when deserializing 'extra' property");

        var result = new List<ChatMessage>();

        // Читаем элементы массива
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            // Если это строка, создаем ChatMessage с этой строкой как Text
            if (reader.TokenType == JsonTokenType.String)
            {
                result.Add(new ChatMessage { Text = reader.GetString() ?? string.Empty });
            }
            // Если это объект, пробуем десериализовать его как ChatMessage
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Сохраняем текущее состояние reader
                var readerAtStart = reader;

                try
                {
                    var chatMessage = JsonSerializer.Deserialize<ChatMessage>(ref reader, options);
                    if (chatMessage != null)
                        result.Add(chatMessage);
                }
                catch (JsonException)
                {
                    // Если не удалось десериализовать как ChatMessage,
                    // пропускаем этот объект и двигаемся к следующему элементу массива
                    SkipToEndObject(ref reader);
                }
            }
        }

        return result.ToArray();
    }

    private void SkipToEndObject(ref Utf8JsonReader reader)
    {
        int depth = 1;

        // Пропускаем все содержимое текущего объекта
        while (depth > 0 && reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
                depth++;
            else if (reader.TokenType == JsonTokenType.EndObject)
                depth--;
        }
    }

    public override void Write(Utf8JsonWriter writer, Description description, JsonSerializerOptions options)
    {
        // Ignore
    }
}
