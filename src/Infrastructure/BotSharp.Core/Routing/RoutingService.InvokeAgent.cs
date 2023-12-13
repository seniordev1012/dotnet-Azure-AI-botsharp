using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    const int MAXIMUM_RECURSION_DEPTH = 3;
    private int _currentRecursionDepth = 0;
    public async Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs)
    {
        _currentRecursionDepth++;
        if (_currentRecursionDepth > MAXIMUM_RECURSION_DEPTH)
        {
            _logger.LogWarning($"Current recursive call depth greater than {MAXIMUM_RECURSION_DEPTH}, which will cause unexpected result.");
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, 
            provider: agent?.LlmConfig?.Provider ?? agentSetting.LlmConfig.Provider,
            model: agent.LlmConfig?.Model ?? agentSetting.LlmConfig.Model);

        var message = dialogs.Last();
        var response = chatCompletion.GetChatCompletions(agent, dialogs);

        if (response.Role == AgentRole.Function)
        {
            message = RoleDialogModel.From(message,
                    role: AgentRole.Function);
            message.FunctionName = response.FunctionName;
            message.FunctionArgs = response.FunctionArgs;
            message.CurrentAgentId = agent.Id;
            await InvokeFunction(message, dialogs);
        }
        else
        {
            message = RoleDialogModel.From(message,
                    role: AgentRole.Assistant,
                    content: response.Content);
            message.CurrentAgentId = agent.Id;
            dialogs.Add(message);
        }

        return true;
    }

    private async Task<bool> InvokeFunction(RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        // execute function
        // Save states
        var states = _services.GetRequiredService<IConversationStateService>();
        states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>());

        var conversationService = _services.GetRequiredService<IConversationService>();
        // Call functions
        await conversationService.CallFunctions(message);

        // Pass execution result to LLM to get response
        if (!message.StopCompletion)
        {
            var routing = _services.GetRequiredService<RoutingContext>();
            
            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var responseTemplate = await templateService.RenderFunctionResponse(message.CurrentAgentId, message);
            if (!string.IsNullOrEmpty(responseTemplate))
            {
                dialogs.Add(RoleDialogModel.From(message,
                    role: AgentRole.Assistant,
                    content: responseTemplate));
            }
            else
            {
                // Save to memory dialogs
                dialogs.Add(RoleDialogModel.From(message, 
                    role: AgentRole.Function, 
                    content: message.Content));

                // Send to Next LLM
                var agentId = routing.GetCurrentAgentId();
                await InvokeAgent(agentId, dialogs);
            }
        }
        else
        {
            dialogs.Add(RoleDialogModel.From(message, 
                role: AgentRole.Assistant, 
                content: message.Content));
        }

        return true;
    }
}
