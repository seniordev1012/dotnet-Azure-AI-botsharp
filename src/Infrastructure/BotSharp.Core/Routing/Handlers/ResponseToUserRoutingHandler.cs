using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ResponseToUserRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "response_to_user";

    public string Description => "Response according to the context without asking specific agent.";

    public List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>
    {
        new ParameterPropertyDef("reason", "why response to user"),
        new ParameterPropertyDef("response", "response content")
    };

    public ResponseToUserRoutingHandler(IServiceProvider services, ILogger<ResponseToUserRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        message.Content = inst.Response;
        message.StopCompletion = true;
        message.Role = AgentRole.Assistant;
        return true;
    }
}
