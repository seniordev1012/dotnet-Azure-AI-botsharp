using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Abstraction.Templating;
using BotSharp.Plugin.RoutingSpeeder.Providers;
using System.Runtime.InteropServices;
using BotSharp.Abstraction.Agents;
using System.IO;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingConversationHook: ConversationHookBase
{
    private readonly IServiceProvider _services;
    private RouterSpeederSettings _settings;
    public RoutingConversationHook(IServiceProvider service, RouterSpeederSettings settings)
    {
        _services = service;
        _settings = settings;
    }
    public override async Task BeforeCompletion(RoleDialogModel message)
    {
        var intentClassifier = _services.GetRequiredService<IntentClassifier>();
        var vector = intentClassifier.GetTextEmbedding(message.Content);

        // intentClassifier.Train();
        // Utilize local discriminative model to predict intent
        var predText = intentClassifier.Predict(vector);

        message.IntentName = predText;

        // Render by template
        var templateService = _services.GetRequiredService<IResponseTemplateService>();
        var response = await templateService.RenderIntentResponse(_agent.Id, message);

        if (!string.IsNullOrEmpty(response))
        {
            message.Content = response;
            message.StopCompletion = true;
        }
    }

    public override async Task AfterCompletion(RoleDialogModel message)
    {
        var routerSettings = _services.GetRequiredService<RoutingSettings>();
        bool saveFlag = (message.CurrentAgentId != routerSettings.RouterId) && (message.CurrentAgentId != routerSettings.ReasonerId);

        if (saveFlag)
        {
            // save train data
            var agentService = _services.CreateScope().ServiceProvider.GetRequiredService<IAgentService>();
            var rootDataPath = agentService.GetDataDir();

            string rawDataDir = Path.Combine(rootDataPath, "raw_data", $"{message.CurrentAgentId}.txt");
            var lastThreeDialogs = _dialogs.Where(x => x.Role == AgentRole.User).Select(x => x.Content).Reverse().Take(3).ToArray();

            if (!File.Exists(rawDataDir))
            {
                await File.WriteAllLinesAsync(rawDataDir, lastThreeDialogs);
            }
            else
            {
                await File.AppendAllLinesAsync(rawDataDir, lastThreeDialogs);
            }
        }
    }
}
