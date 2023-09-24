using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class RouteToAgentRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "route_to_agent";

    public string Description => "Route request to appropriate agent.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("agent_name", "the name of the agent from AGENTS"),
        new NameDesc("reason", "why route to this agent"),
        new NameDesc("args", "parameters extracted from context")
    };

    public bool IsReasoning => false;

    public RouteToAgentRoutingHandler(IServiceProvider services, ILogger<RouteToAgentRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        if (string.IsNullOrEmpty(inst.Route.AgentName))
        {
            inst = await GetNextInstructionFromReasoner($"What's the next step? your response must have agent name.");
        }

        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
        var message = new RoleDialogModel(AgentRole.Function, inst.Question)
        {
            FunctionName = inst.Function,
            FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
            {
                AgentName = inst.Route.AgentName
            }),
        };

        var ret = await function.Execute(message);

        var result = await InvokeAgent(message.CurrentAgentId);
        result.ExecutionData = result.ExecutionData ?? message.ExecutionData;
        
        return result;
    }
}
