using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio
{
    public partial class TextToSpeechProvider : ITextToSpeech
    {
        public string Provider => "openai";
        private readonly IServiceProvider _services;
        private string? _model;

        public TextToSpeechProvider(
            IServiceProvider services)
        {
            _services = services;
        }

        public void SetModelName(string model)
        {
            _model = model;
        }

        public async Task<BinaryData> GenerateSpeechFromTextAsync(string text, ITextToSpeechOptions? options = null)
        {
            var client = ProviderHelper
                .GetClient(Provider, _model, _services)
                .GetAudioClient(_model);
            return await client.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Alloy);
        }
    }
}
