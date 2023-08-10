using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<Agent> CreateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        var record = (from a in db.Agent
                     join ua in db.UserAgent on a.Id equals ua.AgentId
                     join u in db.User on ua.UserId equals u.Id
                     where (ua.UserId == _user.Id || u.ExternalId == _user.Id) && a.Name == agent.Name
                     select a).FirstOrDefault();

        if (record != null)
        {
            return record.ToAgent();
        }

        record = AgentRecord.FromAgent(agent);
        record.Id = Guid.NewGuid().ToString();
        record.CreatedDateTime = DateTime.UtcNow;
        record.UpdatedDateTime = DateTime.UtcNow;

        var userAgentRecord = new UserAgentRecord
        {
            UserId = _user.Id,
            AgentId = record.Id,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        db.Transaction<IBotSharpTable>(delegate
        {
            db.Add<IBotSharpTable>(record);
            db.Add<IBotSharpTable>(userAgentRecord);
        });

        return record.ToAgent();
    }
}
