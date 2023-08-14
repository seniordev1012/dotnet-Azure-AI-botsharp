using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationHook
{
    Agent Agent { get; }
    IConversationHook SetAgent(Agent agent);

    Conversation Conversation { get; }
    IConversationHook SetConversation(Conversation conversation);

    List<RoleDialogModel> Dialogs { get; }
    /// <summary>
    /// Triggered when dialog history is loaded
    /// </summary>
    /// <param name="dialogs"></param>
    /// <returns></returns>
    Task OnDialogsLoaded(List<RoleDialogModel> dialogs);

    Task OnStateLoaded(ConversationState state);
    Task OnStateChanged(string name, string preValue, string currentValue);

    Task BeforeCompletion();
    Task OnFunctionExecuting(RoleDialogModel message);
    Task OnFunctionExecuted(RoleDialogModel message);
    Task AfterCompletion(RoleDialogModel message);
}
