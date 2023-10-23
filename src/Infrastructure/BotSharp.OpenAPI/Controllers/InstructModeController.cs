using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class InstructModeController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;

    public InstructModeController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/instruct/{agentId}")]
    public async Task<InstructResult> InstructCompletion([FromRoute] string agentId,
        [FromBody] InstructMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Split('=')[0], x.Split('=')[1]));
        state.SetState("provider", input.Provider)
            .SetState("model", input.Model)
            .SetState("input_text", input.Text);

        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        // switch to different instruction template
        if (!string.IsNullOrEmpty(input.Template))
        {
            var template = agent.Templates.First(x => x.Name == input.Template).Content;
            var render = _services.GetRequiredService<ITemplateRender>();
            var dict = new Dictionary<string, object>();
            state.GetStates().Select(x => dict[x.Key] = x.Value).ToArray();
            var prompt = render.Render(template, dict);
            agent.Instruction = prompt;
        }

        var instructor = _services.GetRequiredService<IInstructService>();
        return await instructor.Execute(agent,
            new RoleDialogModel(AgentRole.User, input.Text));
    }

    [HttpPost("/instruct/text-completion")]
    public async Task<string> TextCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Split('=')[0], x.Split('=')[1]));
        state.SetState("provider", input.Provider)
            .SetState("model", input.Model);

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        return await textCompletion.GetCompletion(input.Text);
    }
}
