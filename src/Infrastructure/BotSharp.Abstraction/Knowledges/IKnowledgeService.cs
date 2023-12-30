using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

public interface IKnowledgeService
{
    Task Feed(KnowledgeFeedModel knowledge);
    Task EmbedKnowledge(KnowledgeCreationModel knowledge);
    Task<string> GetKnowledges(KnowledgeRetrievalModel retrievalModel);
    Task<List<RetrievedResult>> GetAnswer(KnowledgeRetrievalModel retrievalModel);
}
