using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Plugin.HuggingFace.Services;
using BotSharp.Plugin.HuggingFace.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.HuggingFace.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    public string Provider => "huggingface";

    private readonly IServiceProvider _services;
    private readonly HuggingFaceSettings _settings;
    private readonly ILogger _logger;
    private string _model;

    public ChatCompletionProvider(IServiceProvider services,
        HuggingFaceSettings settings,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var content = string.Join("\r\n", conversations.Select(x => $"{AgentRole.System}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var prompt = agent.Instruction + "\r\n" + content;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(prompt);
        }

        var api = _services.GetRequiredService<IInferenceApi>();

        var space = _model.Split('/')[0];
        var model = _model.Split("/")[1];

        var response = await api.Post(space, model, new InferenceInput
        {
            Inputs = prompt
        });

        var falcon = JsonSerializer.Deserialize<List<FalconLlmResponse>>(response);

        var message = falcon[0].GeneratedText.Trim();
        _logger.LogInformation($"[{agent.Name}] {AgentRole.Assistant}: {message}");

        var msg = new RoleDialogModel(AgentRole.Assistant, message)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        // Text response received
        await onMessageReceived(msg);

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        return true;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var content = string.Join("\r\n", conversations.Select(x => $"{AgentRole.System}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var prompt = agent.Instruction + "\r\n" + content;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(prompt);
        }

        var api = _services.GetRequiredService<IInferenceApi>();

        var space = _model.Split('/')[0];
        var model = _model.Split("/")[1];

        var response = api.Post(space, model, new InferenceInput
        {
            Inputs = prompt
        }).Result;

        var falcon = JsonSerializer.Deserialize<List<FalconLlmResponse>>(response);

        var message = falcon[0].GeneratedText.Trim();
        _logger.LogInformation($"[{agent.Name}] {AgentRole.Assistant}: {message}");

        var msg = new RoleDialogModel(AgentRole.Assistant, message)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return msg;
    }
}
