using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.Loggers.Enums;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.Plugin.ChatHub.Hooks;

public class StreamingLogHook : ConversationHookBase, IContentGeneratingHook
{
    private readonly ConversationSetting _convSettings;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IServiceProvider _services;
    private readonly IHubContext<SignalRHub> _chatHub;
    private readonly IConversationStateService _state;
    private readonly IUserIdentity _user;
    private readonly IAgentService _agentService;

    public StreamingLogHook(
        ConversationSetting convSettings,
        IServiceProvider serivces,
        IHubContext<SignalRHub> chatHub,
        IConversationStateService state,
        IUserIdentity user,
        IAgentService agentService)
    {
        _convSettings = convSettings;
        _services = serivces;
        _chatHub = chatHub;
        _state = state;
        _user = user;
        _agentService = agentService;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            WriteIndented = true
        };
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        var log = $"MessageId: {message.MessageId} ==>\r\n{message.Role}: {message.Content}";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
            BuildContentLog(conversationId, _user.UserName, log, ContentLogSource.UserInput, message));
    }

    public override async Task OnConversationRouting(FunctionCallFromLlm instruct, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        var log = JsonSerializer.Serialize(instruct, _serializerOptions);
        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
                BuildContentLog(conversationId, agent?.Name, log, ContentLogSource.AgentResponse, message));
    }

    public override async Task OnConversationRedirected(string toAgentId, RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        var fromAgent = await _agentService.LoadAgent(message.CurrentAgentId);
        var toAgent = await _agentService.LoadAgent(toAgentId);

        var log = $"{message.Content}\r\n=====\r\nREDIRECTED TO {toAgent.Name}";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
            BuildContentLog(conversationId, fromAgent.Name, log, ContentLogSource.HardRule, message));
    }

    public async Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations)
    {
        if (!_convSettings.ShowVerboseLog) return;

        /*var _state = _services.GetRequiredService<IConversationStateService>();
        var conversationId = _state.GetConversationId();
        var dialog = conversations.Last();
        var log = $"{dialog.Role}: {dialog.Content} [msg_id: {dialog.MessageId}] ==>";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnContentLogGenerated", BuildLog(conversationId, log));*/
    }

    public override async Task OnFunctionExecuted(RoleDialogModel message)
    {
        var conversationId = _state.GetConversationId();
        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        var log = $"{message.FunctionName}({message.FunctionArgs})\r\n    => {message.Content}";
        log += $"\r\n<== MessageId: {message.MessageId}";
        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
            BuildContentLog(conversationId, agent?.Name, log, ContentLogSource.FunctionCall, message));
    }

    /// <summary>
    /// Used to log prompt
    /// </summary>
    /// <param name="message"></param>
    /// <param name="tokenStats"></param>
    /// <returns></returns>
    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        if (!_convSettings.ShowVerboseLog) return;

        var conversationId = _state.GetConversationId();
        var agent = await _agentService.LoadAgent(message.CurrentAgentId);
        var logSource = string.Empty;

        var log = tokenStats.Prompt;
        logSource = ContentLogSource.Prompt;
        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
            BuildContentLog(conversationId, agent?.Name, log, logSource, message));
    }

    /// <summary>
    /// Used to log final response
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        await _chatHub.Clients.User(_user.Id).SendAsync("OnConversateStateLogGenerated", BuildStateLog(conv.ConversationId, _state.GetStates(), message));

        if (message.Role == AgentRole.Assistant)
        {
            var agent = await _agentService.LoadAgent(message.CurrentAgentId);
            var log = $"{message.Content}";
            if (message.RichContent != null && message.RichContent.Message.RichType != "text")
            {
                var richContent = JsonSerializer.Serialize(message.RichContent, _serializerOptions);
                log += $"\r\n{richContent}";
            }
            log += $"\r\n<== MessageId: {message.MessageId}";
            await _chatHub.Clients.User(_user.Id).SendAsync("OnConversationContentLogGenerated",
                BuildContentLog(conv.ConversationId, agent?.Name, log, ContentLogSource.AgentResponse, message));
        }
    }

    private string BuildContentLog(string conversationId, string? name, string logContent, string logSource, RoleDialogModel message)
    {
        var log = new ConversationContentLogModel
        {
            ConversationId = conversationId,
            MessageId = message.MessageId,
            Name = name,
            Role = message.Role,
            Content = logContent,
            Source = logSource,
            CreateTime = DateTime.UtcNow
        };

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableContentLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationContentLog(log);
        }

        return JsonSerializer.Serialize(log, _serializerOptions);
    }

    private string BuildStateLog(string conversationId, Dictionary<string, string> states, RoleDialogModel message)
    {
        var log = new ConversationStateLogModel
        {
            ConversationId = conversationId,
            MessageId = message.MessageId,
            States = states,
            CreateTime = DateTime.UtcNow
        };

        var convSettings = _services.GetRequiredService<ConversationSetting>();
        if (convSettings.EnableStateLog)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            db.SaveConversationStateLog(log);
        }

        return JsonSerializer.Serialize(log, _serializerOptions);
    }
}
