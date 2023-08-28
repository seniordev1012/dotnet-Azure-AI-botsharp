using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Plugin.AzureOpenAI.Settings;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public DeploymentModelSetting DeploymentModel { get; set; } 
        = new DeploymentModelSetting();

    public GPT4Settings GPT4 { get; set; }
}
