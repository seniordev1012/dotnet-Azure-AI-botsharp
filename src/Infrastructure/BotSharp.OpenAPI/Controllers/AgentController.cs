using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IServiceProvider _services;

    public AgentController(IAgentService agentService, IServiceProvider services)
    {
        _agentService = agentService;
        _services = services;
    }

    [HttpGet("/agent/settings")]
    public AgentSettings GetSettings()
    {
        var settings = _services.GetRequiredService<AgentSettings>();
        return settings;
    }

    [HttpGet("/agent/{id}")]
    public async Task<AgentViewModel> GetAgent([FromRoute] string id)
    {
        var agents = await GetAgents(new AgentFilter
        {
            AgentIds = new List<string> { id }
        });
        return agents.Items.First();
    }
             
    [HttpGet("/agents")]
    public async Task<PagedItems<AgentViewModel>> GetAgents([FromQuery] AgentFilter filter)
    {
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        var pagedAgents = await _agentService.GetAgents(filter);

        // prerender agent
        var items = new List<Agent>();
        foreach (var agent in pagedAgents.Items)
        {
            var renderedAgent = await _agentService.LoadAgent(agent.Id);
            items.Add(renderedAgent);
        }

        // Set IsHost
        var agents = items.Select(x => AgentViewModel.FromAgent(x)).ToList();
        foreach(var agent in agents)
        {
            agent.IsHost = agentSetting.HostAgentId == agent.Id;
        }

        return new PagedItems<AgentViewModel>
        {
            Items = agents,
            Count = pagedAgents.Count
        };
    }

    [HttpPost("/agent")]
    public async Task<AgentViewModel> CreateAgent(AgentCreationModel agent)
    {
        var createdAgent = await _agentService.CreateAgent(agent.ToAgent());
        return AgentViewModel.FromAgent(createdAgent);
    }

    [HttpPost("/refresh-agents")]
    public async Task RefreshAgents()
    {
        await _agentService.RefreshAgents();
    }

    [HttpPut("/agent/file/{agentId}")]
    public async Task UpdateAgentFromFile([FromRoute] string agentId)
    {
        await _agentService.UpdateAgentFromFile(agentId);
    }

    [HttpPut("/agent/{agentId}")]
    public async Task UpdateAgent([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.All);
    }

    [HttpPatch("/agent/{agentId}/{field}")]
    public async Task PatchAgentByField([FromRoute] string agentId, AgentField field, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, field);
    }
}