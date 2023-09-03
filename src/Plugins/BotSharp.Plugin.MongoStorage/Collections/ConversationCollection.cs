namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationCollection : MongoBase
{
    public Guid AgentId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
