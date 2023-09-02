namespace BotSharp.Abstraction.Repositories.Records;

public class RoutingItemRecord : RecordBase
{
    public string AgentId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> RequiredFields { get; set; } = new List<string>();
    public string RedirectTo { get; set; }
    public bool Disabled { get; set; }
}
