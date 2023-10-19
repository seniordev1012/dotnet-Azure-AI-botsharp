using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;

namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    [JsonIgnore]
    public string Instruction { get; set; }

    /// <summary>
    /// Templates
    /// </summary>
    [JsonIgnore]
    public List<AgentTemplate>? Templates { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    [JsonIgnore]
    public List<string> Samples { get; set; }

    /// <summary>
    /// Functions
    /// </summary>
    [JsonIgnore]
    public List<FunctionDef> Functions { get; set; } = new List<FunctionDef>();

    /// <summary>
    /// Responses
    /// </summary>
    [JsonIgnore]
    public List<AgentResponse>? Responses { get; set; }

    /// <summary>
    /// Domain knowledges
    /// </summary>
    [JsonIgnore]
    public string Knowledges { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>
    /// Allow to be routed
    /// </summary>
    public bool AllowRouting {  get; set; }

    public bool Disabled { get; set; }

    /// <summary>
    /// Profile by channel
    /// </summary>
    public List<string> Profiles { get; set; }
        = new List<string>();

    public List<RoutingRule> RoutingRules { get; set; }
        = new List<RoutingRule>();

    public override string ToString()
        => $"{Name} {Id}";


    public static Agent Clone(Agent agent)
    {
        return new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Responses = agent.Responses,
            Samples = agent.Samples,
            Knowledges = agent.Knowledges,
            IsPublic = agent.IsPublic,
            Disabled = agent.Disabled,
            AllowRouting = agent.AllowRouting,
            Profiles = agent.Profiles,
            RoutingRules = agent.RoutingRules,
            CreatedDateTime = agent.CreatedDateTime,
            UpdatedDateTime = agent.UpdatedDateTime,
        };
    }

    public Agent SetInstruction(string instruction)
    {
        Instruction = instruction;
        return this;
    }

    public Agent SetTemplates(List<AgentTemplate> templates)
    {
        Templates = templates ?? new List<AgentTemplate>();
        return this;
    }

    public Agent SetFunctions(List<FunctionDef> functions)
    {
        Functions = functions ?? new List<FunctionDef>();
        return this;
    }

    public Agent SetSamples(List<string> samples)
    {
        Samples = samples ?? new List<string>();
        return this;
    }

    public Agent SetResponses(List<AgentResponse> responses)
    {
        Responses = responses ?? new List<AgentResponse>(); ;
        return this;
    }

    public Agent SetId(string id)
    {
        Id = id;
        return this;
    }

    public Agent SetName(string name)
    {
        Name = name;
        return this;
    }

    public Agent SetDescription(string description)
    {
        Description = description;
        return this;
    }

    public Agent SetIsPublic(bool isPublic)
    {
        IsPublic = isPublic;
        return this;
    }

    public Agent SetDisabled(bool disabled)
    {
        Disabled = disabled;
        return this;
    }

    public Agent SetAllowRouting(bool allowRouting)
    {
        AllowRouting = allowRouting;
        return this;
    }

    public Agent SetProfiles(List<string> profiles)
    {
        Profiles = profiles ?? new List<string>();
        return this;
    }

    public Agent SetRoutingRules(List<RoutingRule> rules)
    {
        RoutingRules = rules ?? new List<RoutingRule>();
        return this;
    }
}
