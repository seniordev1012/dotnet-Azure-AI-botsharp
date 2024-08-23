using BotSharp.Plugin.AudioHandler.Settings;
using BotSharp.Plugin.AudioHandler.Provider;
using BotSharp.Abstraction.Settings;

namespace BotSharp.Plugin.AudioHandler
{
    public class AudioHandlerPlugin : IBotSharpPlugin
    {
        public string Id => "9d22014c-4f45-466a-9e82-a74e67983df8";
        public string Name => "Audio Handler";
        public string Description => "Process audio input and transform it into text output.";
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            //var settings = new AudioHandlerSettings();
            //config.Bind("AudioHandler", settings);
            //services.AddSingleton(x => settings);

            services.AddScoped(provider =>
            {
                var settingService = provider.GetRequiredService<ISettingService>();
                return settingService.Bind<AudioHandlerSettings>("AudioHandler");
            });

            services.AddScoped<ISpeechToText, NativeWhisperProvider>();
            services.AddScoped<IAudioProcessUtilities, AudioProcessUtilities>();
            services.AddScoped<IAgentHook, AudioHandlerHook>();
            services.AddScoped<IAgentUtilityHook, AudioHandlerUtilityHook>();
        }
    }
}

