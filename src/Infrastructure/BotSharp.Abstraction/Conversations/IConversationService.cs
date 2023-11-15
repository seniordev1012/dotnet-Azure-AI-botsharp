namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    IConversationStateService States { get; }
    string ConversationId { get; }
    Task<Conversation> NewConversation(Conversation conversation);
    void SetConversationId(string conversationId, List<string> states);
    Task<Conversation> GetConversation(string id);
    Task<List<Conversation>> GetConversations();
    Task<List<Conversation>> GetLastConversations();
    Task DeleteConversation(string id);

    /// <summary>
    /// Send message to LLM
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="conversationId"></param>
    /// <param name="lastDalog"></param>
    /// <param name="onMessageReceived"></param>
    /// <param name="onFunctionExecuting">This delegate is useful when you want to report progress on UI</param>
    /// <param name="onFunctionExecuted">This delegate is useful when you want to report progress on UI</param>
    /// <returns></returns>
    Task<bool> SendMessage(string agentId, 
        RoleDialogModel lastDalog, 
        Func<RoleDialogModel, Task> onMessageReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted);

    List<RoleDialogModel> GetDialogHistory(int lastCount = 20);
    Task CleanHistory(string agentId);

    Task CallFunctions(RoleDialogModel msg);
}
