using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Evaluations;
using BotSharp.Abstraction.Evaluations.Models;
using BotSharp.Abstraction.Evaluations.Settings;
using BotSharp.Abstraction.Templating;
using System.Drawing;

namespace BotSharp.Core.Evaluatings;

public class EvaluatingService : IEvaluatingService
{
    private readonly IServiceProvider _services;
    private readonly EvaluatorSetting _settings;
    public EvaluatingService(IServiceProvider services, EvaluatorSetting settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<Conversation> Execute(string task, EvaluationRequest request)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var evaluator = await agentService.GetAgent(_settings.EvaluatorId);
        // Task execution mode
        evaluator.Instruction = evaluator.Templates.First(x => x.Name == "instruction.executor").Content;
        var taskPrompt = evaluator.Templates.First(x => x.Name == $"task.{task}").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(evaluator.Instruction, new Dictionary<string, object>
        {
            { "task_prompt",  taskPrompt}
        });

        var service = _services.GetRequiredService<IConversationService>();
        var conv = await service.NewConversation(new Conversation
        {
            AgentId = request.AgentId
        });

        var result = new EvaluationResult
        {
            TaskInstruction = taskPrompt,
            SystemPrompt = evaluator.Instruction
        };

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        RoleDialogModel response = new RoleDialogModel(AgentRole.User, "");
        var dialogs = new List<RoleDialogModel>();
        int roundCount = 0;
        while (true)
        {
            // var text = string.Join("\r\n", dialogs.Select(x => $"{x.Role}: {x.Content}"));
            // text = instruction + $"\r\n###\r\n{text}\r\n{AgentRole.User}: ";
            var question = await textCompletion.GetCompletion(prompt, request.AgentId, response.MessageId);
            dialogs.Add(new RoleDialogModel(AgentRole.User, question));
            prompt += question.Trim();

            response = await SendMessage(request.AgentId, conv.Id, question);
            dialogs.Add(new RoleDialogModel(AgentRole.Assistant, response.Content));
            prompt += $"\r\n{AgentRole.Assistant}: {response.Content.Trim()}";
            prompt += $"\r\n{AgentRole.User}: ";

            roundCount++;

            if (roundCount > 10)
            {
                Console.WriteLine($"Conversation ended due to execced max round count {roundCount}", Color.Red);
                break;
            }

            if (response.FunctionName == "conversation_end" ||
                response.FunctionName == "human_intervention_needed")
            {
                Console.WriteLine($"Conversation ended by function {response.FunctionName}", Color.Green);
                break;
            }
        }

        result.Dialogs = dialogs;
        return conv;
    }

    public async Task<EvaluationResult> Evaluate(string conversationId, EvaluationRequest request)
    {
        throw new NotImplementedException();
    }

    private async Task<RoleDialogModel> SendMessage(string agentId, string conversationId, string text)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, new List<string>());

        RoleDialogModel response = default;

        await conv.SendMessage(agentId,
            ConversationChannel.OpenAPI,
            new RoleDialogModel(AgentRole.User, text),
            async msg => response = msg,
            fnExecuting => Task.CompletedTask,
            fnExecuted => Task.CompletedTask);

        return response;
    }

    public Task<EvaluationResult> Review(string conversationId, EvaluationRequest request)
    {
        throw new NotImplementedException();
    }
}
