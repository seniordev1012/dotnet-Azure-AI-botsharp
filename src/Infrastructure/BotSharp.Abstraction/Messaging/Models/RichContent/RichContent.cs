namespace BotSharp.Abstraction.Messaging.Models.RichContent
{
    public class RichContent<T>
    {
        public Recipient Recipient { get; set; } = new Recipient();
        /// <summary>
        /// RESPONSE
        /// </summary>
        [JsonPropertyName("messaging_type")]
        public string MessagingType => "RESPONSE";
        public T Message { get; set; }
    }
}
