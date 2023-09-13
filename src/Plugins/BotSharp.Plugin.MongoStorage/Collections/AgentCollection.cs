using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentCollection : MongoBase
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public List<AgentTemplate> Templates { get; set; }
    public List<string> Functions { get; set; }
    public List<AgentResponse> Responses { get; set; }
    public bool IsPublic { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}