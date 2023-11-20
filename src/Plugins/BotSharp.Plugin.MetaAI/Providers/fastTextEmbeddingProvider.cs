using BotSharp.Abstraction.MLTasks;
using BotSharp.Plugin.MetaAI.Settings;
using FastText.NetWrapper;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaAI.Providers;

public class fastTextEmbeddingProvider : ITextEmbedding
{
    private FastTextWrapper _fastText;
    private readonly fastTextSetting _settings;

    public int Dimension
    {
        get
        {
            LoadModel();
            return _fastText.GetModelDimension();
        }
    }

    public fastTextEmbeddingProvider(fastTextSetting settings)
    {
        _settings = settings;

    }

    public Task<float[]> GetVectorAsync(string text)
    {
        LoadModel();
        return Task.FromResult(_fastText.GetSentenceVector(text));
    }

    public async Task<List<float[]>> GetVectorsAsync(List<string> texts)
    {
        LoadModel();
        var vectors = new List<float[]>();
        for (int i = 0; i < texts.Count; i++)
        {
            vectors.Add(await GetVectorAsync(texts[i]));
        }
        return vectors;
    }

    private void LoadModel()
    {
        if (_fastText == null)
        {
            if (!File.Exists(_settings.ModelPath))
            {
                throw new FileNotFoundException($"Can't load pre-trained word vectors from {_settings.ModelPath}.\n Try to download from https://fasttext.cc/docs/en/english-vectors.html.");
            }

            _fastText = new FastTextWrapper();

            if (!_fastText.IsModelReady())
            {
                _fastText.LoadModel(_settings.ModelPath);
            }
        }
    }
}
