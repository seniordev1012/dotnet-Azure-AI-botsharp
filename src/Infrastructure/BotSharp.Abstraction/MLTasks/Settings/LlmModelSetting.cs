namespace BotSharp.Abstraction.MLTasks.Settings;

public class LlmModelSetting
{
    /// <summary>
    /// Model Id, like "gpt-3.5" and "gpt-4".
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Deployment model name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Deployment same functional model in a group.
    /// It can be used to deploy same model in different regions.
    /// </summary>
    public string? Group { get; set; }

    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
    public LlmModelType Type { get; set; } = LlmModelType.Chat;

    /// <summary>
    /// Prompt cost per 1K token
    /// </summary>
    public float PromptCost { get; set; }

    /// <summary>
    /// Completion cost per 1K token
    /// </summary>
    public float CompletionCost { get; set; }

    public override string ToString()
    {
        return $"[{Type}] {Name} {Endpoint}";
    }
}

public enum LlmModelType
{
    Text = 1,
    Chat = 2
}
