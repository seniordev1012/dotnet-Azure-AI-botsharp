
namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class QuickReplyMessage : IMessageTemplate
    {
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("quick_replies")]
        public List<QuickReplyElement> QuickReplies { get; set; } = new List<QuickReplyElement>();
    }
}
