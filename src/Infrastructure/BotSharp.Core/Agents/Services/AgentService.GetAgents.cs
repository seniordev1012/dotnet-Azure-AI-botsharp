using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<Agent>> GetAgents()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from a in db.Agents
                    join ua in db.UserAgents on a.Id equals ua.AgentId
                    join u in db.Users on ua.UserId equals u.Id
                    where ua.UserId == _user.Id || u.ExternalId == _user.Id || a.IsPublic
                    select a;
        return query.ToList();
    }

    [MemoryCache(10 * 60)]
    public async Task<Agent> GetAgent(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var profile = db.GetAgent(id);

        var instructionFile = profile?.Instruction;
        if (instructionFile != null)
        {
            profile.Instruction = instructionFile;
        }
        else
        {
            _logger.LogError($"Can't find instruction file from {instructionFile}");
        }

        var samplesFile = profile?.Samples;
        if (samplesFile != null)
        {
            profile.Samples = samplesFile;
        }

        var functionsFile = profile?.Functions;
        if (functionsFile != null)
        {
            profile.Functions = functionsFile;
        }

        return profile;
    }
}
