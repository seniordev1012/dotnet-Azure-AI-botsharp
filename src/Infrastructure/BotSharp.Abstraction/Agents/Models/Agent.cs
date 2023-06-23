namespace BotSharp.Abstraction.Agents.Models;

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }

    /// <summary>
    /// Instruction
    /// </summary>
    public string Instruction { get; set; }

    /// <summary>
    /// Samples
    /// </summary>
    public string Samples { get; set; }

    /// <summary>
    /// Owner user id
    /// </summary>
    public string OwerId { get; set; } = string.Empty;
}
