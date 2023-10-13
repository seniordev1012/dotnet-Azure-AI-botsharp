using BotSharp.Abstraction.Routing.Models;
using System.Text.Json;

namespace BotSharp.Abstraction.Functions.Models;

public class FunctionCallFromLlm : RoutingArgs
{
    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("args")]
    public JsonDocument Arguments { get; set; } = JsonDocument.Parse("{}");

    public override string ToString()
    {
        var route = string.IsNullOrEmpty(AgentName) ? "" : $"<Route to {AgentName.ToUpper()} because {Reason}>";

        if (string.IsNullOrEmpty(Response))
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question}";
        }
        else
        {
            return $"[{Function} {route} {JsonSerializer.Serialize(Arguments)}]: {Question} => {Response}";
        }
    }
}
