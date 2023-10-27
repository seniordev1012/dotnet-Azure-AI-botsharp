using BotSharp.Abstraction.Models;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class MessageResponseModel : ITrackableMessage
{
    public string MessageId { get; set; }
    public string Text { get; set; }
    public string Function { get; set; }
    public object Data { get; set; }
}
