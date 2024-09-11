namespace BotSharp.Plugin.KnowledgeBase.Helpers;

public static class KnowledgeSettingHelper
{
    public static ITextEmbedding GetTextEmbeddingSetting(IServiceProvider services, string collectionName)
    {
        var settings = services.GetRequiredService<KnowledgeBaseSettings>();
        var db = services.GetRequiredService<IBotSharpRepository>();

        // Get collection config from db
        var configs = db.GetKnowledgeCollectionConfigs(new VectorCollectionConfigFilter
        {
            CollectionNames = [collectionName],
            VectorStroageProviders = [settings.VectorDb.Provider]
        });

        var found = configs?.FirstOrDefault()?.TextEmbedding;
        var provider = found?.Provider ?? string.Empty;
        var model = found?.Model ?? string.Empty;
        var dimension = found?.Dimension ?? 0;

        if (found == null)
        {
            provider = settings.Default.TextEmbedding.Provider;
            model = settings.Default.TextEmbedding.Model;
            dimension = settings.Default.TextEmbedding.Dimension;
        }

        // Set up text embedding
        var embedding = services.GetServices<ITextEmbedding>().FirstOrDefault(x => x.Provider == provider);

        if (dimension <= 0)
        {
            dimension = GetLlmTextEmbeddingDimension(services, provider, model);
        }

        embedding.SetModelName(model);
        embedding.SetDimension(dimension);
        return embedding;
    }

    private static int GetLlmTextEmbeddingDimension(IServiceProvider services, string provider, string model)
    {
        var settings = services.GetRequiredService<ILlmProviderService>();
        var found = settings.GetSetting(provider, model);
        return found?.Dimension ?? 0;
    }
}
