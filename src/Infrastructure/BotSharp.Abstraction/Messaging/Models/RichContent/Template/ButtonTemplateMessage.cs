namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    /// <summary>
    /// https://developers.facebook.com/docs/messenger-platform/send-messages/buttons
    /// </summary>
    public class ButtonTemplateMessage : IRichMessage
    {
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("template_type")]
        public string TemplateType => "button";
        public List<ButtonElement> Buttons { get; set; } = new List<ButtonElement>();
    }

    public class ButtonElement
    {
        /// <summary>
        /// web_url, postback, phone_number
        /// </summary>
        public string Type { get; set; } = "web_url";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Payload { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}
