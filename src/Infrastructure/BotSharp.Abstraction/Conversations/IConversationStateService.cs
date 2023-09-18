namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Conversation state service to track the context in the conversation lifecycle
/// </summary>
public interface IConversationStateService
{
    ConversationState Load(string conversationId);
    string GetState(string name, string defaultValue = "");
    ConversationState GetStates();
    IConversationStateService SetState<T>(string name, T value);
    void CleanState();
    void Save();
}
