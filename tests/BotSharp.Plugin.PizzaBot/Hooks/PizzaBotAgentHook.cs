using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.PizzaBot.Hooks;

public class PizzaBotAgentHook : AgentHookBase
{
    public PizzaBotAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        return base.OnInstructionLoaded(template, dict);
    }
}
