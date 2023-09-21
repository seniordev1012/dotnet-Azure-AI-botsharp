using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Templating;
using System.IO;

namespace BotSharp.Core.Routing;

public class RoutingService : IRoutingService
{
    private readonly IServiceProvider _services;
    private readonly RoutingSettings _settings;
    private readonly ILogger _logger;
    private List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public RoutingService(IServiceProvider services,
        RoutingSettings settings,
        ILogger<RoutingService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<RoleDialogModel> Enter(Agent router, List<RoleDialogModel> whileDialogs)
    {
        _dialogs = new List<RoleDialogModel>();
        RoleDialogModel result = new RoleDialogModel(AgentRole.Assistant, "not handled");

        foreach (var dialog in whileDialogs.TakeLast(20))
        {
            router.Instruction += $"\r\n{dialog.Role}: {dialog.Content}";
        }

        var inst = await GetNextInstructionFromReasoner($"What's the next step to make user's original goal?", router);
        int loopCount = 0;
        while (loopCount < 3)
        {
            loopCount++;
            if (inst.Function == "continue_execute_task")
            {
                var routing = _services.GetRequiredService<IAgentRouting>();
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.Agents.First(x => x.Name.ToLower() == inst.Parameters.AgentName.ToLower());

                result = new RoleDialogModel(AgentRole.Function, inst.Parameters.Question)
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(inst.Parameters.Arguments),
                    CurrentAgentId = record.Id,
                };
                break;
            }
            // Compatible with previous Router, can be removed in the future.
            else if (inst.Function == "route_to_agent")
            {
                // If the agent name is empty, fallback to router
                if (string.IsNullOrEmpty(inst.Parameters.AgentName))
                {
                    result = new RoleDialogModel(AgentRole.Function, inst.Reason)
                    {
                        FunctionName = inst.Function,
                        FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
                        {
                            AgentName = inst.Parameters.AgentName
                        }),
                        CurrentAgentId = router.Id
                    };
                    break;
                }

                var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == inst.Function);
                result = new RoleDialogModel(AgentRole.Function, inst.Reason)
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(new RoutingArgs
                    {
                        AgentName = inst.Parameters.AgentName
                    }),
                };
                var ret = await function.Execute(result);
                break;
            }
            else if (inst.Function == "interrupt_task_execution")
            {
                result = new RoleDialogModel(AgentRole.User, inst.Reason)
                {
                    FunctionName = inst.Function
                };
                break;
            }
            else if (inst.Function == "response_to_user")
            {
                result = new RoleDialogModel(AgentRole.User, inst.Parameters.Answer)
                {
                    FunctionName = inst.Function
                };
                break;
            }
            else if (inst.Function == "retrieve_data_from_agent")
            {
                // Retrieve information from specific agent
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.Agents.First(x => x.Name.ToLower() == inst.Parameters.AgentName.ToLower());
                var response = await RetrieveDataFromAgent(record.Id, new List<RoleDialogModel>
                {
                    new RoleDialogModel(AgentRole.User, inst.Parameters.Question)
                });

                inst.Parameters.Answer = response.Content;

                _dialogs.Add(new RoleDialogModel(AgentRole.Assistant, inst.Parameters.Question)
                {
                    CurrentAgentId = record.Id
                });

                router.Instruction += $"\r\n{AgentRole.Assistant}: {inst.Parameters.Question}";

                _dialogs.Add(new RoleDialogModel(AgentRole.Function, inst.Parameters.Answer)
                {
                    FunctionName = inst.Function,
                    FunctionArgs = JsonSerializer.Serialize(inst.Parameters.Arguments),
                    ExecutionResult = inst.Parameters.Answer,
                    ExecutionData = response.ExecutionData,
                    CurrentAgentId = record.Id
                });

                router.Instruction += $"\r\n{AgentRole.Function}: {response.Content}";

                // Got the response from agent, then send to reasoner again to make the decision
                inst = await GetNextInstructionFromReasoner($"What's the next step based on user's original goal and function result?", router);
            }
        }

        return result;
    }

    private async Task<FunctionCallFromLlm> GetNextInstructionFromReasoner(string prompt, Agent reasoner)
    {
        var responseFormat = JsonSerializer.Serialize(new FunctionCallFromLlm());
        var wholeDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, $"{prompt} Response in JSON format {responseFormat}")
        };

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, 
            provider: _settings.Provider, 
            model: _settings.Model);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(reasoner, wholeDialogs, async msg
            => response = msg, fn
            => Task.CompletedTask);

        var args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);

        if (args.Parameters.Arguments != null)
        {
            SaveStateByArgs(args.Parameters.Arguments);
        }

        args.Function = args.Function.Split('.').Last();
        args.Parameters.AgentName = args.Parameters.AgentName.Split(':').Last().Trim();

        _logger.LogInformation($"*** Next Instruction *** {args}");

        return args;
    }

    private async Task<RoleDialogModel> RetrieveDataFromAgent(string agentId, List<RoleDialogModel> wholeDialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg
            => response = msg, async fn
            =>
            {
                // execute function
                // Save states
                SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(fn.FunctionArgs));

                var conversationService = _services.GetRequiredService<IConversationService>();
                // Call functions
                await conversationService.CallFunctions(fn);

                response = fn;
                response.Content = fn.ExecutionResult;
            });
        return response;
    }

    private void SaveStateByArgs(JsonDocument args)
    {
        if (args == null)
        {
            return;
        }

        var stateService = _services.GetRequiredService<IConversationStateService>();
        if (args.RootElement is JsonElement root)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (!string.IsNullOrEmpty(property.Value.ToString()))
                {
                    stateService.SetState(property.Name, property.Value);
                }
            }
        }
    }

    public Agent LoadRouter()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var router = new Agent()
        {
            Id = _settings.RouterId,
            Name = _settings.RouterName,
            Description = _settings.Description
        };
        var agents = db.Agents.Where(x => !x.Disabled && x.AllowRouting).ToArray();

        var dict = new Dictionary<string, object>();
        dict["routing_records"] = agents.Select(x => new RoutingItem
        {
            AgentId = x.Id,
            Description = x.Description,
            Name = x.Name,
            RequiredFields = x.RoutingRules.Where(x => x.Required)
                .Select(x => x.Field)
                .ToArray()
        }).ToArray();

        dict["enable_reasoning"] = _settings.EnableReasoning;
        if (_settings.EnableReasoning)
        {
            dict["reasoning_functions"] = PromptConst.REASONING_FUNCTIONS;
        }

        var render = _services.GetRequiredService<ITemplateRender>();
        router.Instruction = render.Render(PromptConst.ROUTER_PROMPT, dict);

        return router;
    }
}
