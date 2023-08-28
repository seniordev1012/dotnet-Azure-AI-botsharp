using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Utilities;
using BotSharp.Plugin.AzureOpenAI.Providers;
using BotSharp.Plugin.AzureOpenAI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BotSharp.Platform.AzureAi;

public class AzureOpenAiPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new AzureOpenAiSettings();
        config.Bind("AzureOpenAi", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded AzureOpenAi settings: {settings.DeploymentModel} ({settings.Endpoint}) {settings.ApiKey.SubstringMax(4)}");
            return settings;
        });

        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<IChatCompletion, GPT4CompletionProvider>();
    }
}