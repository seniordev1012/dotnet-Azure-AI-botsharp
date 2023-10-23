using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public ConversationController(IServiceProvider services, 
        IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost("/conversation/{agentId}")]
    public async Task<ConversationViewModel> NewConversation([FromRoute] string agentId)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var sess = new Conversation
        {
            AgentId = agentId
        };
        sess = await service.NewConversation(sess);
        return ConversationViewModel.FromSession(sess);
    }

    [HttpDelete("/conversation/{agentId}/{conversationId}")]
    public async Task DeleteConversation([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<MessageResponseModel> SendMessage([FromRoute] string agentId, 
        [FromRoute] string conversationId, 
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, input.States);
        conv.States.SetState("channel", input.Channel)
            .SetState("provider", input.Provider)
            .SetState("model", input.Model)
            .SetState("temperature", input.Temperature)
            .SetState("sampling_factor", input.SamplingFactor);

        var response = new MessageResponseModel();
        var stackMsg = new List<RoleDialogModel>();

        await conv.SendMessage(agentId,
            new RoleDialogModel("user", input.Text),
            async msg =>
            {
                stackMsg.Add(msg);
            },
            async fnExecuting =>
            {

            },
            async fnExecuted =>
            {
                response.Function = fnExecuted.FunctionName;
                response.Data = fnExecuted.Data;
            });

        response.Text = string.Join("\r\n", stackMsg.Select(x => x.Content));
        response.Data = response.Data ?? stackMsg.Last().Data;
        response.Function = stackMsg.Last().FunctionName;

        return response;
    }
}
