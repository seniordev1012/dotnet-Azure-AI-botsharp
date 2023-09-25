using BotSharp.Abstraction.Routing.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm : RoutingArgs
{
    [JsonPropertyName("function")]
    public string Function { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; } = JsonDocument.Parse("{}");

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";

        if (string.IsNullOrEmpty(Answer))
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question}";
        }
        else
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question} => {Answer}";
        }
    }
}
