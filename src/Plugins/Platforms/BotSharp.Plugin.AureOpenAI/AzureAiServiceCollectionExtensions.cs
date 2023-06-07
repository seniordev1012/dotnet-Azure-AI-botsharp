using BotSharp.Abstraction.TextGeneratives;
using BotSharp.Platform.AzureAi;
using BotSharp.Plugin.AzureOpenAI.TextGeneratives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core;

public static class AzureAiServiceCollectionExtensions
{
    public static IServiceCollection AddAzureOpenAiPlatform(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(x =>
        {
            var settings = new AzureAiSettings();
            config.Bind("AzureAi", settings);
            return settings;
        });
        services.AddScoped<IChatCompletionProvider, ChatCompletionProvider>();
        return services;
    }
}