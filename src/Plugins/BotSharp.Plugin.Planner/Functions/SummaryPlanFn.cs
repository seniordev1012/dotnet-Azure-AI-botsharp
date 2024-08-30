using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Templating;
using System.Threading.Tasks;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.Planner.Functions;

public class SummaryPlanFn : IFunctionCallback
{
    public string Name => "plan_summary";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private object aiAssistant;

    public SummaryPlanFn(IServiceProvider services, ILogger<PrimaryStagePlanFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        //debug
        var state = _services.GetRequiredService<IConversationStateService>();
        state.SetState("max_tokens", "4096");

        var task = state.GetState("requirement_detail");

        // summarize and generate query
        var summaryPlanningPrompt = await GetPlanSummaryPrompt(task, message);
        _logger.LogInformation(summaryPlanningPrompt);

        var plannerAgent = new Agent
        {
            Id = BuiltInAgentId.Planner,
            Name = "planner_summary",
            Instruction = summaryPlanningPrompt,
            TemplateDict = new Dictionary<string, object>()
        };
        var response_summary = await GetAiResponse(plannerAgent);

        message.Content = response_summary.Content;
        message.StopCompletion = true;

        return true;
    }

    private async Task<string> GetPlanSummaryPrompt(string task, RoleDialogModel message)
    {
        // save to knowledge base
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var aiAssistant = await agentService.GetAgent(BuiltInAgentId.Planner);
        var template = aiAssistant.Templates.FirstOrDefault(x => x.Name == "two_stage.summarize")?.Content ?? string.Empty;
        var responseFormat = JsonSerializer.Serialize(new FirstStagePlan
        {
            Parameters = [JsonDocument.Parse("{}")],
            Results = [""]
        });

        return render.Render(template, new Dictionary<string, object>
        {
            { "table_structure", message.SecondaryContent }, ////check
            { "task_description", task},
            { "relevant_knowledges", message.Content },
            { "response_format", responseFormat }
        });
    }
    private async Task<RoleDialogModel> GetAiResponse(Agent plannerAgent)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var wholeDialogs = conv.GetDialogHistory();

        //add "test" to wholeDialogs' last element
        if (plannerAgent.Name == "planner_summary")
        {
            //add "test" to wholeDialogs' last element in a new paragraph
            wholeDialogs.Last().Content += "\n\nIf the table structure didn't mention auto incremental, the data field id needs to insert id manually and you need to use max(id) instead of LAST_INSERT_ID function.\nFor example, you should use SET @id = select max(id) from table;";
            wholeDialogs.Last().Content += "\n\nTry if you can generate a single query to fulfill the needs";
        }

        if (plannerAgent.Name == "planning_1st")
        {
            //add "test" to wholeDialogs' last element in a new paragraph
            wholeDialogs.Last().Content += "\n\nYou must analyze the table description to infer the table relations.";
        }

        var completion = CompletionProvider.GetChatCompletion(_services, 
            provider: plannerAgent.LlmConfig.Provider, 
            model: plannerAgent.LlmConfig.Model);

        return await completion.GetChatCompletions(plannerAgent, wholeDialogs);
    }
}
