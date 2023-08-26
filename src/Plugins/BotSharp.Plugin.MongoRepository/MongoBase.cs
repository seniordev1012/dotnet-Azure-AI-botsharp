using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace BotSharp.Plugin.MongoRepository;

[BsonIgnoreExtraElements(Inherited = true)]
public class MongoBase
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    public string Id { get; set; }
}
