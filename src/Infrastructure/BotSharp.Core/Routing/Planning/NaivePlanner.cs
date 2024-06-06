using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing.Planning;

public class NaivePlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public NaivePlanner(IServiceProvider services, ILogger<NaivePlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var next = GetNextStepPrompt(router);

        var inst = new FunctionCallFromLlm();

        // text completion
        /*var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(router);
        var content = $"{instruction}\r\n###\r\n{next}";
        content =  content + "\r\nResponse: ";
        var completion = CompletionProvider.GetTextCompletion(_services);*/

        // chat completion
        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: router?.LlmConfig?.Provider,
            model: router?.LlmConfig?.Model);

        int retryCount = 0;
        while (retryCount < 3)
        {
            string text = string.Empty;
            try
            {
                // text completion
                // text = await completion.GetCompletion(content, router.Id, messageId);
                dialogs = new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, next)
                    {
                        FunctionName = nameof(NaivePlanner),
                        MessageId = messageId
                    }
                };
                var response = await completion.GetChatCompletions(router, dialogs);

                inst = response.Content.JsonContent<FunctionCallFromLlm>();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}: {text}");
                inst.Function = "response_to_user";
                inst.Response = ex.Message;
                inst.AgentName = "Router";
            }
            finally
            {
                retryCount++;
            }
        }

        // Fix LLM malformed response
        FixMalformedResponse(inst);

        return inst;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        // Set user content as Planner's question
        message.FunctionName = inst.Function;
        message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);

        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();
        if (inst.UnmatchedAgent)
        {
            var unmatchedAgentId = context.GetCurrentAgentId();

            // Exclude the wrong routed agent
            var agents = router.TemplateDict["routing_agents"] as RoutableAgent[];
            router.TemplateDict["routing_agents"] = agents.Where(x => x.AgentId != unmatchedAgentId).ToArray();

            // Handover to Router;
            context.Pop();
        }
        else
        {
            context.Empty(reason: $"Agent queue is cleared by {nameof(NaivePlanner)}");
            // context.Push(inst.OriginalAgent, "Push user goal agent");
        }
        return true;
    }

    private string GetNextStepPrompt(Agent router)
    {
        var template = router.Templates.First(x => x.Name == "planner_prompt.naive").Content;

        var states = _services.GetRequiredService<IConversationStateService>();
        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { StateConst.EXPECTED_ACTION_AGENT,  states.GetState(StateConst.EXPECTED_ACTION_AGENT) },
            { StateConst.EXPECTED_GOAL_AGENT,  states.GetState(StateConst.EXPECTED_GOAL_AGENT) }
        });
    }

    /// <summary>
    /// Sometimes LLM hallucinates and fails to set function names correctly.
    /// </summary>
    /// <param name="args"></param>
    private void FixMalformedResponse(FunctionCallFromLlm args)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = agentService.GetAgents(new AgentFilter
        {
            Type = AgentType.Task
        }).Result.Items.ToList();
        var malformed = false;

        // Sometimes it populate malformed Function in Agent name
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function == args.AgentName)
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Another case of malformed response
        if (string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.Function))
        {
            args.AgentName = args.Function;
            args.Function = "route_to_agent";
            malformed = true;
        }

        // It should be Route to agent, but it is used as Response to user.
        if (!string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.AgentName) &&
            args.Function != "route_to_agent")
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Function name shouldn't contain dot symbol
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function.Contains('.'))
        {
            args.Function = args.Function.Split('.').Last();
            malformed = true;
        }

        // Agent Name is contaminated.
        if (args.Function == "route_to_agent")
        {
            // Action agent name
            if (!agents.Any(x => x.Name == args.AgentName))
            {
                args.AgentName = agents.FirstOrDefault(x => args.AgentName.Contains(x.Name))?.Name ?? args.AgentName;
            }

            // Goal agent name
            if (!agents.Any(x => x.Name == args.OriginalAgent))
            {
                args.OriginalAgent = agents.FirstOrDefault(x => args.OriginalAgent.Contains(x.Name))?.Name ?? args.OriginalAgent;
            }
        }

        if (malformed)
        {
            _logger.LogWarning($"Captured LLM malformed response");
        }
    }
}
