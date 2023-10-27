using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Templating;
using System.Drawing;
using System.Text.RegularExpressions;
namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<FunctionCallFromLlm> GetNextInstruction()
    {
        var content = GetNextStepPrompt();

        RoleDialogModel response = default;
        var args = new FunctionCallFromLlm();

        if (_settings.UseTextCompletion)
        {
            var completion = CompletionProvider.GetTextCompletion(_services,
                provider: _settings.Provider,
                model: _settings.Model);

            content = _routerInstance.Router.Instruction + "\r\n\r\n" + content + "\r\nResponse: ";

            int retryCount = 0;

            while (retryCount < 3)
            {
                try
                {
                    var text = await completion.GetCompletion(content);
                    response = new RoleDialogModel(AgentRole.Assistant, text);

                    var pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
                    response.Content = Regex.Match(response.Content, pattern).Value;
                    args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}: {response.Content}");
                    args.Function = "response_to_user";
                    args.Response = ex.Message;
                    args.AgentName = "Router";
                    content += "\r\nPlease response in JSON format.";
                }
                finally
                {
                    retryCount++;
                }
            }
        }
        else
        {
            var completion = CompletionProvider.GetChatCompletion(_services,
                provider: _settings.Provider,
                model: _settings.Model);

            int retryCount = 0;
            var agentService = _services.GetRequiredService<IAgentService>();
            var dialogs = Dialogs;

            while (retryCount < 3)
            {
                try
                {
                    var conversation = "";

                    foreach (var dialog in dialogs.TakeLast(50))
                    {
                        var role = dialog.Role;
                        if (role != AgentRole.User)
                        {
                            var agent = await agentService.GetAgent(dialog.CurrentAgentId);
                            role = agent.Name;
                        }
                        
                        conversation += $"{role}: {dialog.Content}\r\n";
                    }
                    content = $"{conversation}\r\n###\r\n{content}";

                    response = completion.GetChatCompletions(_routerInstance.Router, new List<RoleDialogModel>
                    {
                        new RoleDialogModel(AgentRole.User, content)
                    });

                    args = response.Content.JsonContent<FunctionCallFromLlm>();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}: {response.Content}");
                    args.Function = "response_to_user";
                    args.Response = ex.Message;
                    args.AgentName = "Router";
                    content += "\r\nPlease response in JSON format.";
                }
                finally 
                { 
                    retryCount++; 
                }
            }
        }

#if DEBUG
        Console.WriteLine(response.Content, Color.Green);
#else
        _logger.LogInformation(response.Content);
#endif

        // Fix LLM malformed response
        FixMalformedResponse(args);

        SaveStateByArgs(args.Arguments);

#if DEBUG
        Console.WriteLine($"*** Next Instruction *** {args}", Color.Green);
#else
        _logger.LogInformation($"*** Next Instruction *** {args}");
#endif

        return args;
    }

    private string GetNextStepPrompt()
    {
        var template = _routerInstance.Router.Templates.First(x => x.Name == "next_step_prompt").Content;

        // If enabled reasoning
        // JsonSerializer.Serialize(new FunctionCallFromLlm());

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "enabled_reasoning", _settings.EnableReasoning }
        });
    }

    /// <summary>
    /// Sometimes LLM hallucinates and fails to set function names correctly.
    /// </summary>
    /// <param name="args"></param>
    private void FixMalformedResponse(FunctionCallFromLlm args)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = agentService.GetAgents(allowRouting: true).Result;
        var malformed = false;

        // Sometimes it populate malformed Function in Agent name
        if (!string.IsNullOrEmpty(args.Function) && 
            args.Function == args.AgentName)
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Another case of malformed response
        if (string.IsNullOrEmpty(args.AgentName) && 
            agents.Select(x => x.Name).Contains(args.Function))
        {
            args.AgentName = args.Function;
            args.Function = "route_to_agent";
            malformed = true;
        }

        // It should be Route to agent, but it is used as Response to user.
        if (!string.IsNullOrEmpty(args.AgentName) &&
            agents.Select(x => x.Name).Contains(args.AgentName) &&
            args.Function != "route_to_agent")
        {
            args.Function = "route_to_agent";
            malformed = true;
        }

        // Function name shouldn't contain dot symbol
        if (!string.IsNullOrEmpty(args.Function) &&
            args.Function.Contains('.'))
        {
            args.Function = args.Function.Split('.').Last();
            malformed = true;
        }

        if (malformed)
        {
            _logger.LogWarning($"Captured LLM malformed response");
        }
    }
}
