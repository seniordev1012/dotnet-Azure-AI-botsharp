using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;
using LLama;
using LLama.Common;

namespace BotSharp.Core.Plugins.LLamaSharp;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    public ChatCompletionProvider(IServiceProvider services)
    {
        _services = services;
    }

    public string GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        string totalResponse = "";
        var content = string.Join("\n", conversations.Select(x => $"{x.Role}: {x.Content.Replace("user:", "")}")).Trim();
        content += "\nassistant: ";

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel();
        var executor = new StatelessExecutor(llama.Model);
        var inferenceParams = new InferenceParams() { Temperature = 1.0f, AntiPrompts = new List<string> { "user:" }, MaxTokens = 64 };

        foreach (var response in executor.Infer(agent.Instruction, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        return true;
    }
}
