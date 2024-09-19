namespace BotSharp.Abstraction.Knowledges.Models;

public class KnowledgeFileFilter : Pagination
{
    public IEnumerable<Guid>? FileIds { get; set; }

    public IEnumerable<string>? FileSources { get; set; }
}
