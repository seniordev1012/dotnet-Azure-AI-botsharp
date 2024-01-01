namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationHistoryState : Dictionary<string, List<HistoryStateValue>>
{
    public ConversationHistoryState()
    {

    }

    public ConversationHistoryState(List<HistoryStateKeyValue> pairs)
    {
        foreach (var pair in pairs)
        {
            this[pair.Key] = pair.Values;
        }
    }
}
