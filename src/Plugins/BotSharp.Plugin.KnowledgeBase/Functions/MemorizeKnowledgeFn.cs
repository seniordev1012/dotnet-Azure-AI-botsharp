using BotSharp.Abstraction.Functions;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class MemorizeKnowledgeFn : IFunctionCallback
{
    public string Name => "memorize_knowledge";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public MemorizeKnowledgeFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        var embedding = _services.GetServices<ITextEmbedding>()
            .First(x => x.GetType().FullName.EndsWith(_settings.TextEmbedding));

        var vector = await embedding.GetVectorsAsync(new List<string>
        {
            args.Question
        });

        var vectorDb = _services.GetRequiredService<IVectorDb>();

        await vectorDb.CreateCollection("lessen", vector[0].Length);

        var id = Utilities.HashTextMd5(args.Question);
        var result = await vectorDb.Upsert("lessen", id, vector[0], 
            args.Question, 
            new Dictionary<string, string> 
            { 
                { "answer", args.Answer } 
            });

        message.Content = result ? "Saved to my brain" : "I forgot it";

        return true;
    }
}
