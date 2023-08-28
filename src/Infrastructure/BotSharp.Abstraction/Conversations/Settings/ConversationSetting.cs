namespace BotSharp.Abstraction.Conversations.Settings;

public class ConversationSetting
{
    public string DataDir { get; set; }
    public string ChatCompletion { get; set; }
    public bool EnableKnowledgeBase { get; set; }
    public bool ShowVerboseLog { get; set; }
    public int MaxRecursiveDepth { get; set; } = 3;
    public bool EnableReasoning { get; set; }
}
