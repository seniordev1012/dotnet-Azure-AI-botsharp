using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Settings;

namespace BotSharp.Core.Infrastructures;

public class LlmProviderService : ILlmProviderService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public LlmProviderService(IServiceProvider services, ILogger<LlmProviderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public List<string> GetProviders()
    {
        var providers = new List<string>();
        var services1 = _services.GetServices<ITextCompletion>();
        providers.AddRange(services1.Select(x => x.Provider));

        var services2 = _services.GetServices<IChatCompletion>();
        providers.AddRange(services2.Select(x => x.Provider));

        var services3 = _services.GetServices<ITextEmbedding>();
        providers.AddRange(services3.Select(x => x.Provider));

        return providers.Distinct().ToList();
    }

    public List<LlmModelSetting> GetProviderModels(string provider)
    {
        var settingService = _services.GetRequiredService<ISettingService>();
        return settingService.Bind<List<LlmProviderSetting>>($"LlmProviders")
            .FirstOrDefault(x => x.Provider.Equals(provider))
            ?.Models ?? new List<LlmModelSetting>();
    }

    public LlmModelSetting? GetSetting(string provider, string model)
    {
        var settings = _services.GetRequiredService<List<LlmProviderSetting>>();
        var providerSetting = settings.FirstOrDefault(p => p.Provider.Equals(provider, StringComparison.CurrentCultureIgnoreCase));
        if (providerSetting == null)
        {
            _logger.LogError($"Can't find provider settings for {provider}");
            return null;
        }

        var modelSetting = providerSetting.Models.FirstOrDefault(m => m.Name.Equals(model, StringComparison.CurrentCultureIgnoreCase));
        if (modelSetting == null)
        {
            _logger.LogError($"Can't find model settings for {provider}.{model}");
            return null;
        }

        return modelSetting;
    }
}
