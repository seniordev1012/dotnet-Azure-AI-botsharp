using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.MLTasks;

public interface IChatCompletion
{
    string GetChatCompletions(Agent agent, List<RoleDialogModel> conversations);
    Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived);
}
