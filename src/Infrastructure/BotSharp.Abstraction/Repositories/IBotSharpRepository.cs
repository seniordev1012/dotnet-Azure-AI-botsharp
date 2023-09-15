using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Repositories;

public interface IBotSharpRepository
{
    IQueryable<User> Users { get; }
    IQueryable<Agent> Agents { get; }
    IQueryable<UserAgent> UserAgents { get; }
    IQueryable<Conversation> Conversations { get; }
    IQueryable<RoutingItem> RoutingItems { get; }
    IQueryable<RoutingProfile> RoutingProfiles { get; }

    int Transaction<TTableInterface>(Action action);
    void Add<TTableInterface>(object entity);

    #region User
    User GetUserByEmail(string email);
    void CreateUser(User user);
    #endregion

    #region Agent
    void UpdateAgent(Agent agent, AgentField field);
    Agent GetAgent(string agentId);
    List<string> GetAgentResponses(string agentId, string prefix, string intent);
    string GetAgentTemplate(string agentId, string templateName);
    #endregion

    #region Conversation
    void CreateNewConversation(Conversation conversation);
    string GetConversationDialog(string conversationId);
    void UpdateConversationDialog(string conversationId, string dialogs);
    List<StateKeyValue> GetConversationStates(string conversationId);
    void UpdateConversationStates(string conversationId, List<StateKeyValue> states);
    Conversation GetConversation(string conversationId);
    List<Conversation> GetConversations(string userId);
    #endregion

    #region Routing
    List<RoutingItem> CreateRoutingItems(List<RoutingItem> routingItems);
    List<RoutingProfile> CreateRoutingProfiles(List<RoutingProfile> profiles);
    void DeleteRoutingItems();
    void DeleteRoutingProfiles();
    #endregion    
}
