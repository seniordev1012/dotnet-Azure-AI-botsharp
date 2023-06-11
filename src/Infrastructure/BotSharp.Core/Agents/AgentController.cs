using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Core.Agents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Agents;

[Authorize]
[ApiController]
public class AgentController : ControllerBase, IApiAdapter
{
    private readonly IAgentService _agentService;
    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("/agent")]
    public async Task<string> CreateAgent(AgentCreationModel agent)
    {
        return await _agentService.CreateAgent(agent.ToAgent());
    }
}