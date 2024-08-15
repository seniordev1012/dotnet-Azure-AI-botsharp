using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MetaAI.Providers;

public class FaissDb : IVectorDb
{
    public string Name => "Faiss";

    public Task CreateCollection(string collectionName, int dim)
    {
        throw new NotImplementedException();
    }

    public Task<StringIdPagedItems<KnowledgeCollectionData>> GetCollectionData(string collectionName, KnowledgeFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetCollections()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<KnowledgeCollectionData>> Search(string collectionName, float[] vector,
        IEnumerable<string>? fields, int limit = 10, float confidence = 0.5f, bool withVector = false)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Upsert(string collectionName, string id, float[] vector, string text, Dictionary<string, string>? payload = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteCollectionData(string collectionName, string id)
    {
        throw new NotImplementedException();
    }
}
