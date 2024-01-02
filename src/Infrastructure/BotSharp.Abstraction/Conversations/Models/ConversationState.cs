namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationState : Dictionary<string, List<StateValue>>
{
    public ConversationState()
    {

    }

    public ConversationState(List<StateKeyValue> pairs)
    {
        foreach (var pair in pairs)
        {
            this[pair.Key] = pair.Values;
        }
    }
}
