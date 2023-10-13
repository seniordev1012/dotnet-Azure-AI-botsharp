using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using System.Drawing;

namespace BotSharp.Core.Routing;

/// <summary>
/// Router calls this function to set the Active Agent according to the context
/// </summary>
public class RouteToAgentFn : IFunctionCallback
{
    public string Name => "route_to_agent";
    private readonly IServiceProvider _services;
    private readonly RoutingContext _context;

    public RouteToAgentFn(IServiceProvider services, RoutingContext context)
    {
        _services = services;
        _context = context;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);

        // Push original task agent
        if (!string.IsNullOrEmpty(args.OriginalAgent) && args.OriginalAgent.Length < 32)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var originalAgent = db.Agents.FirstOrDefault(x => x.Name.ToLower() == args.OriginalAgent.ToLower());
            if (originalAgent != null)
            {
                _context.Push(originalAgent.Id);
            }
        }
        else
        {
            // Push current agent to routing stack
            _context.Push(message.CurrentAgentId);
        }

        if (string.IsNullOrEmpty(args.AgentName))
        {
            message.ExecutionResult = $"missing agent name";
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var targetAgent = db.Agents.FirstOrDefault(x => x.Name.ToLower() == args.AgentName.ToLower());
            if (targetAgent == null)
            {
                message.ExecutionData = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
                return false;
            }

            var missingfield = HasMissingRequiredField(message, out var agentId);
            if (missingfield && message.CurrentAgentId != agentId)
            {
                // Stack original Agent
                _context.Push(targetAgent.Id);

                message.CurrentAgentId = agentId;
            }
            else
            {
                message.CurrentAgentId = targetAgent.Id;
                message.ExecutionResult = $"Routing to {args.AgentName}";
            }
        }

        _context.Push(message.CurrentAgentId);

        // Set default execution data
        message.ExecutionData = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        return true;
    }

    /// <summary>
    /// If the target agent needs some required fields but the
    /// </summary>
    /// <returns></returns>
    private bool HasMissingRequiredField(RoleDialogModel message, out string agentId)
    {
        var args = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs);
        var router = _services.GetRequiredService<IRouterInstance>();

        var routingRules = router.GetRulesByName(args.AgentName);

        if (routingRules == null || !routingRules.Any())
        {
            agentId = message.CurrentAgentId;
            return false;
        }

        agentId = routingRules.First().AgentId;
        // Add routed agent
        message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "route_to", agentId);

        // Check required fields
        var root = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        var missingFields = new List<string>();
        foreach (var field in routingRules.Where(x => x.Required).Select(x => x.Field))
        {
            if (!root.EnumerateObject().Any(x => x.Name == field))
            {
                missingFields.Add(field);
            }
            else if (root.EnumerateObject().Any(x => x.Name == field) &&
                string.IsNullOrEmpty(root.EnumerateObject().FirstOrDefault(x => x.Name == field).Value.ToString()))
            {
                missingFields.Add(field);
            }
        }

        // Check if states contains the field according conversation context.
        var states = _services.GetRequiredService<IConversationStateService>();
        foreach (var field in missingFields.ToList())
        {
            if (!string.IsNullOrEmpty(states.GetState(field)))
            {
                var value = states.GetState(field);
                message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, field, value);
                missingFields.Remove(field);
            }
        }

        if (missingFields.Any())
        {
            // Add field to args
            message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "missing_fields", missingFields);
            message.ExecutionResult = $"missing some information: {string.Join(',', missingFields)}";
            message.Content = message.ExecutionResult;

            // Handle redirect
            var routingRule = routingRules.FirstOrDefault(x => missingFields.Contains(x.Field));
            if (!string.IsNullOrEmpty(routingRule.RedirectTo))
            {
                var db = _services.GetRequiredService<IBotSharpRepository>();
                var record = db.Agents.First(x => x.Id == routingRule.RedirectTo);

                // Add redirected agent
                message.FunctionArgs = AppendPropertyToArgs(message.FunctionArgs, "redirect_to", record.Name);
                agentId = routingRule.RedirectTo;
                var logger = _services.GetRequiredService<ILogger<RouteToAgentFn>>();
#if DEBUG
                Console.WriteLine($"*** Routing redirect to {record.Name.ToUpper()} ***", Color.Yellow);
#else
                logger.LogInformation($"*** Routing redirect to {record.Name.ToUpper()} ***");
#endif
            }
            else
            {
                // back to router
                agentId = message.CurrentAgentId;
            }
        }

        return missingFields.Any();
    }

    private string AppendPropertyToArgs(string args, string key, string value)
    {
        return args.Substring(0, args.Length - 1) + $", \"{key}\": \"{value}\"" + "}";
    }

    private string AppendPropertyToArgs(string args, string key, IEnumerable<string> values)
    {
        string fields = string.Join(",", values.Select(x => $"\"{x}\""));
        return args.Substring(0, args.Length - 1) + $", \"{key}\": [{fields}]" + "}";
    }
}
