namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilter : StringIdPagination
{
    [JsonPropertyName("with_vector")]
    public bool WithVector { get; set; }

    /// <summary>
    /// For keyword search
    /// </summary>
    [JsonPropertyName("search_pairs")]
    public IEnumerable<KeyValue>? SearchPairs { get; set; }


    /// <summary>
    /// Included payload keys
    /// </summary>
    [JsonPropertyName("included_payloads")]
    public IEnumerable<string>? IncludedPayloads { get; set; }
}