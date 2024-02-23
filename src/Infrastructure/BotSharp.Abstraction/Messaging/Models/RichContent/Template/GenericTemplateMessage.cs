namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template;

public class GenericTemplateMessage<T> : IRichMessage, ITemplateMessage
{
    [JsonPropertyName("rich_type")]
    public string RichType => "generic_template";

    [JsonIgnore]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("template_type")]
    public string TemplateType => "generic";

    [JsonPropertyName("elements")]
    public List<T> Elements { get; set; } = new List<T>();
}

public class GenericElement
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }
    [JsonPropertyName("default_action")]
    public ElementAction DefaultAction { get; set; }
    public ElementButton[] Buttons { get; set; }
}
