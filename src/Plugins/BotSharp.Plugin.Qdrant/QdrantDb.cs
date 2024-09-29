using BotSharp.Abstraction.Utilities;
using BotSharp.Abstraction.VectorStorage.Models;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace BotSharp.Plugin.Qdrant;

public class QdrantDb : IVectorDb
{
    private QdrantClient _client;
    private readonly QdrantSetting _setting;
    private readonly IServiceProvider _services;
    private readonly ILogger<QdrantDb> _logger;

    public QdrantDb(
        QdrantSetting setting,
        ILogger<QdrantDb> logger,
        IServiceProvider services)
    {
        _setting = setting;
        _logger = logger;
        _services = services;
    }

    public string Provider => "Qdrant";

    private QdrantClient GetClient()
    {
        if (_client == null)
        {
            _client = new QdrantClient
            (
                host: _setting.Url,
                https: true,
                apiKey: _setting.ApiKey
            );
        }
        return _client;
    }

    public async Task<bool> DoesCollectionExist(string collectionName)
    {
        var client = GetClient();
        return await client.CollectionExistsAsync(collectionName);
    }

    public async Task<bool> CreateCollection(string collectionName, int dimension)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (exist) return false;

        try
        {
            // Create a new collection
            var client = GetClient();
            await client.CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = (ulong)dimension,
                Distance = Distance.Cosine
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when create collection (Name: {collectionName}, Dimension: {dimension}).");
            return false;
        }
    }

    public async Task<bool> DeleteCollection(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);

        if (!exist) return false;

        var client = GetClient();
        await client.DeleteCollectionAsync(collectionName);
        return true;
    }

    public async Task<IEnumerable<string>> GetCollections()
    {
        // List all the collections
        var collections = await GetClient().ListCollectionsAsync();
        return collections.ToList();
    }

    public async Task<StringIdPagedItems<VectorCollectionData>> GetPagedCollectionData(string collectionName, VectorFilter filter)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return new StringIdPagedItems<VectorCollectionData>();
        }

        // Build query filter
        Filter? queryFilter = null;
        if (!filter.SearchPairs.IsNullOrEmpty())
        {
            var conditions = filter.SearchPairs.Select(x => new Condition
            {
                Field = new FieldCondition
                {
                    Key = x.Key,
                    Match = new Match { Text = x.Value },
                }
            });

            queryFilter = new Filter
            {
                Should =
                {
                    conditions
                }
            };
        }

        // Build payload selector
        WithPayloadSelector? payloadSelector = null;
        if (!filter.IncludedPayloads.IsNullOrEmpty())
        {
            payloadSelector = new WithPayloadSelector
            { 
                Enable = true,
                Include = new PayloadIncludeSelector
                {
                    Fields = { filter.IncludedPayloads.ToArray() }
                }
            };
        }

        var client = GetClient();
        var totalPointCount = await client.CountAsync(collectionName, filter: queryFilter);
        var response = await client.ScrollAsync(collectionName, limit: (uint)filter.Size, 
            offset: !string.IsNullOrWhiteSpace(filter.StartId) ? new PointId { Uuid = filter.StartId } : null,
            filter: queryFilter,
            payloadSelector: payloadSelector,
            vectorsSelector: filter.WithVector);

        var points = response?.Result?.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                _ => new object()
            }),
            Vector = filter.WithVector ? x.Vectors?.Vector?.Data?.ToArray() : null
        })?.ToList() ?? new List<VectorCollectionData>();

        return new StringIdPagedItems<VectorCollectionData>
        {
            Count = totalPointCount,
            NextId = response?.NextPageOffset?.Uuid,
            Items = points
        };
    }


    public async Task<IEnumerable<VectorCollectionData>> GetCollectionData(string collectionName, IEnumerable<Guid> ids,
        bool withPayload = false, bool withVector = false)
    {
        if (ids.IsNullOrEmpty())
        {
            return Enumerable.Empty<VectorCollectionData>();
        }
        
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return Enumerable.Empty<VectorCollectionData>();
        }

        var client = GetClient();
        var pointIds = ids.Select(x => new PointId { Uuid = x.ToString() }).Distinct().ToList();
        var points = await client.RetrieveAsync(collectionName, pointIds, withPayload, withVector);
        return points.Select(x => new VectorCollectionData
        {
            Id = x.Id?.Uuid ?? string.Empty,
            Data = x.Payload?.ToDictionary(p => p.Key, p => p.Value.KindCase switch 
            { 
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                _ => new object()
            }) ?? new(),
            Vector = x.Vectors?.Vector?.Data?.ToArray()
        });
    }

    public async Task<bool> Upsert(string collectionName, Guid id, float[] vector, string text, Dictionary<string, object>? payload = null)
    {
        // Insert vectors
        var point = new PointStruct()
        {
            Id = new PointId()
            {
                Uuid = id.ToString()
            },
            Vectors = vector,
            Payload =
            {
                { KnowledgePayloadName.Text, text }
            }
        };

        if (payload != null)
        {
            foreach (var item in payload)
            {
                if (item.Value is string str)
                {
                    point.Payload[item.Key] = str;
                }
                else if (item.Value is bool b)
                {
                    point.Payload[item.Key] = b;
                }
                else if (item.Value is byte int8)
                {
                    point.Payload[item.Key] = int8;
                }
                else if (item.Value is short int16)
                {
                    point.Payload[item.Key] = int16;
                }
                else if (item.Value is int int32)
                {
                    point.Payload[item.Key] = int32;
                }
                else if (item.Value is long int64)
                {
                    point.Payload[item.Key] = int64;
                }
                else if (item.Value is float f32)
                {
                    point.Payload[item.Key] = f32;
                }
                else if (item.Value is double f64)
                {
                    point.Payload[item.Key] = f64;
                }
                else if (item.Value is DateTime dt)
                {
                    point.Payload[item.Key] = dt.ToUniversalTime().ToString("o");
                }
            }
        }

        var client = GetClient();
        var result = await client.UpsertAsync(collectionName, points: new List<PointStruct>
        {
            point
        });

        return result.Status == UpdateStatus.Completed;
    }

    public async Task<IEnumerable<VectorCollectionData>> Search(string collectionName, float[] vector,
        IEnumerable<string>? fields, int limit = 5, float confidence = 0.5f, bool withVector = false)
    {
        var results = new List<VectorCollectionData>();

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return results;
        }

        var payloadSelector = new WithPayloadSelector { Enable = true };
        if (fields != null)
        {
            payloadSelector.Include = new PayloadIncludeSelector { Fields = { fields.ToArray() } };
        }

        var client = GetClient();
        var points = await client.SearchAsync(collectionName,
                                            vector,
                                            limit: (ulong)limit,
                                            scoreThreshold: confidence,
                                            payloadSelector: payloadSelector,
                                            vectorsSelector: withVector);

        results = points.Select(x => new VectorCollectionData
        {
            Id = x.Id.Uuid,
            Data = x.Payload.ToDictionary(p => p.Key, p => p.Value.KindCase switch
            {
                Value.KindOneofCase.StringValue => p.Value.StringValue,
                Value.KindOneofCase.BoolValue => p.Value.BoolValue,
                Value.KindOneofCase.IntegerValue => p.Value.IntegerValue,
                _ => new object()
            }),
            Score = x.Score,
            Vector = x.Vectors?.Vector?.Data?.ToArray()
        }).ToList();

        return results;
    }

    public async Task<bool> DeleteCollectionData(string collectionName, List<Guid> ids)
    {
        if (ids.IsNullOrEmpty()) return false;

        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = GetClient();
        var result = await client.DeleteAsync(collectionName, ids);
        return result.Status == UpdateStatus.Completed;
    }

    public async Task<bool> DeleteCollectionAllData(string collectionName)
    {
        var exist = await DoesCollectionExist(collectionName);
        if (!exist)
        {
            return false;
        }

        var client = GetClient();
        var result = await client.DeleteAsync(collectionName, new Filter());
        return result.Status == UpdateStatus.Completed;
    }
}
