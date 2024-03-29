using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ResponseToUserRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "response_to_user";

    public string Description => "Response according to the context without asking specific agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why response to user"),
        new ParameterPropertyDef("response", "response content"),
        new ParameterPropertyDef("conversation_end", "whether to end this conversation, true or false", type: "boolean")
    };

    public ResponseToUserRoutingHandler(IServiceProvider services, ILogger<ResponseToUserRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        var response = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            StopCompletion = true
        };

        _dialogs.Add(response);

        return true;
    }
}
