using System.IO;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextEmbeddingProvider : ITextEmbedding
{
    private LLamaEmbedder _embedder;
    private readonly LlamaSharpSettings _settings;
    private readonly IServiceProvider _services;
    public int Dimension => 4096;

    public TextEmbeddingProvider(IServiceProvider services, LlamaSharpSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public Task<float[]> GetVectorAsync(string text)
    {
        if (_embedder == null)
        {
            var path = Path.Combine(_settings.ModelDir, _settings.DefaultModel);
            _embedder = new LLamaEmbedder(new ModelParams(path));
        }

        return Task.FromResult(_embedder.GetEmbeddings(text));
    }

    public Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        throw new NotImplementedException();
    }
}
