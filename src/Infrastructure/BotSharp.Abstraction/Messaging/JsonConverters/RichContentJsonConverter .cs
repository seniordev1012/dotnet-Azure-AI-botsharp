using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using System.Text.Json;

namespace BotSharp.Abstraction.Messaging.JsonConverters;

public class RichContentJsonConverter : JsonConverter<IRichMessage>
{
    public override IRichMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;
        var jsonText = root.GetRawText();
        JsonElement element;
        object? res = null;

        if (root.TryGetProperty("buttons", out element))
        {
            res = JsonSerializer.Deserialize<ButtonTemplateMessage>(jsonText, options);
        }
        else if (root.TryGetProperty("options", out element))
        {
            res = JsonSerializer.Deserialize<MultiSelectTemplateMessage>(jsonText, options);
        }

        return res as IRichMessage;
    }

    public override void Write(Utf8JsonWriter writer, IRichMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
