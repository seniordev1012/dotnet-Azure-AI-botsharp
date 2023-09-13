using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Agents;

public interface IAgentRouting
{
    string AgentId { get; }
    Task<Agent> LoadRouter();
    RoutingItem[] GetRoutingRecords();
    RoutingItem GetRecordByAgentId(string id);
    RoutingItem GetRecordByName(string name);
}
