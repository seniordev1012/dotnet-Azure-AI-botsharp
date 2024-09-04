namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilter : StringIdPagination
{
    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    [JsonPropertyName("search_pairs")]
    public IEnumerable<KeyValue>? SearchPairs { get; set; }
}