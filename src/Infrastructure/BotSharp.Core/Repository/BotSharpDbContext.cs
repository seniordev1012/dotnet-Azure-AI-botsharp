using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Users.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BotSharp.Core.Repository;

public class BotSharpDbContext : Database, IBotSharpRepository
{
    public IQueryable<User> Users => throw new NotImplementedException();

    public IQueryable<Agent> Agents => throw new NotImplementedException();

    public IQueryable<UserAgent> UserAgents => throw new NotImplementedException();

    public IQueryable<Conversation> Conversations => throw new NotImplementedException();


    public int Transaction<TTableInterface>(Action action)
    {
        DatabaseFacade database = base.GetMaster(typeof(TTableInterface)).Database;
        int num = 0;
        if (database.CurrentTransaction == null)
        {
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction dbContextTransaction = database.BeginTransaction())
            {
                try
                {
                    action();
                    num = base.SaveChanges();
                    dbContextTransaction.Commit();
                    return num;
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    if (ex.Message.Contains("See the inner exception for details"))
                    {
                        throw ex.InnerException;
                    }

                    throw ex;
                }
            }
        }

        try
        {
            action();
            return base.SaveChanges();
        }
        catch (Exception ex2)
        {
            if (database.CurrentTransaction != null)
            {
                database.CurrentTransaction.Rollback();
            }

            if (ex2.Message.Contains("See the inner exception for details"))
            {
                throw ex2.InnerException;
            }

            throw ex2;
        }
    }


    #region Agent
    public Agent GetAgent(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<Agent> GetAgents(string? name = null, bool? disabled = null, bool? allowRouting = null,
        bool? isPublic = null, List<string>? agentIds = null)
    {
        throw new NotImplementedException();
    }

    public List<Agent> GetAgentsByUser(string userId)
    {
        throw new NotImplementedException();
    }

    public void UpdateAgent(Agent agent, AgentField field)
    {
        throw new NotImplementedException();
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        throw new NotImplementedException();
    }

    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        throw new NotImplementedException();
    }

    public void BulkInsertAgents(List<Agent> agents)
    {
        throw new NotImplementedException();
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
        throw new NotImplementedException();
    }

    public bool DeleteAgents()
    {
        throw new NotImplementedException();
    }
    #endregion


    #region Conversation
    public void CreateNewConversation(Conversation conversation)
    {
        throw new NotImplementedException();
    }

    public Conversation GetConversation(string conversationId)
    {
        throw new NotImplementedException();
    }

    public List<Conversation> GetConversations(string userId)
    {
        throw new NotImplementedException();
    }

    public List<Conversation> GetLastConversations()
    {
        throw new NotImplementedException();
    }

    public string GetConversationDialog(string conversationId)
    {
        throw new NotImplementedException();
    }

    public List<StateKeyValue> GetConversationStates(string conversationId)
    {
        throw new NotImplementedException();
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        throw new NotImplementedException();
    }
    public void UpdateConversationTitle(string conversationId, string title)
    {
        throw new NotImplementedException();
    }
    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        throw new NotImplementedException();
    }

    public void UpdateConversationStatus(string conversationId, string status)
    {
        throw new NotImplementedException();
    }

    public void AddExectionLogs(string conversationId, List<string> logs)
    {
        throw new NotImplementedException();
    }

    public List<string> GetExectionLogs(string conversationId)
    {
        throw new NotImplementedException();
    }
    #endregion


    #region User
    public User? GetUserByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public User? GetUserById(string id)
    {
        throw new NotImplementedException();
    }

    public void CreateUser(User user)
    {
        throw new NotImplementedException();
    }
    #endregion
}
