namespace BotSharp.Plugin.Twilio.Models
{
    public class AssistantMessage
    {
        public bool ConversationEnd { get; set; }
        public string Content { get; set; }
        public string MessageId { get; set; }
        public string SpeechFileName { get; set; }
    }
}
