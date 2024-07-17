using BotSharp.Abstraction.Functions;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class KnowledgeRetrievalFn : IFunctionCallback
{
    public string Name => "knowledge_retrieval";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public KnowledgeRetrievalFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        var embedding =  _services.GetServices<ITextEmbedding>()
            .FirstOrDefault(x => x.GetType().FullName.EndsWith(_settings.TextEmbedding));

        var vector = await embedding.GetVectorsAsync(new List<string>
        {
            args.Question
        });

        var vectorDb = _services.GetRequiredService<IVectorDb>();

        var id = Utilities.HashTextMd5(args.Question);
        var knowledges = await vectorDb.Search("lessen", vector[0], "answer");

        message.Content = string.Join("\r\n\r\n=====\r\n", knowledges);

        return true;
    }
}
