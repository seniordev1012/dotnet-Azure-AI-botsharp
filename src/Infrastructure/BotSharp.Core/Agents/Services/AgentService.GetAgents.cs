using BotSharp.Abstraction.Agents.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<Agent>> GetAgents()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from a in db.Agent
                    join ua in db.UserAgent on a.Id equals ua.AgentId
                    join u in db.User on ua.UserId equals u.Id
                    where ua.UserId == _user.Id || u.ExternalId == _user.Id
                    select a.ToAgent();
        return query.ToList();
    }

    public async Task<Agent> GetAgent(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from agent in db.Agent
                    where agent.Id == id
                    select agent.ToAgent();

        var profile = query.FirstOrDefault();
        var dir = GetAgentDataDir(id);

        var instructionFile = Path.Combine(dir, $"instruction.{_settings.TemplateFormat}");
        if (File.Exists(instructionFile))
        {
            profile.Instruction = File.ReadAllText(instructionFile);
        }
        else
        {
            _logger.LogError($"Can't find instruction file from {instructionFile}");
        }

        var samplesFile = Path.Combine(dir, $"samples.{_settings.TemplateFormat}");
        if (File.Exists(samplesFile))
        {
            profile.Samples = File.ReadAllText(samplesFile);
        }
        var functionsFile = Path.Combine(dir, "functions.json");
        if (File.Exists(functionsFile))
        {
            profile.Functions = File.ReadAllText(functionsFile);
        }

        return profile;
    }
}
