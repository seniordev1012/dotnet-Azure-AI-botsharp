using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage.Repository;

public class MongoRepository : IBotSharpRepository
{
    private readonly MongoDbContext _dc;
    private readonly IServiceProvider _services;
    private UpdateOptions _options;

    public MongoRepository(MongoDbContext dc, IServiceProvider services)
    {
        _dc = dc;
        _services = services;
        _options = new UpdateOptions
        {
            IsUpsert = true,
        };
    }

    private List<AgentRecord> _agents;
    public IQueryable<AgentRecord> Agent
    {
        get
        {
            if (_agents != null)
            {
                return _agents.AsQueryable();
            }

            var agentDocs = _dc.Agents?.AsQueryable()?.ToList() ?? new List<AgentCollection>();
            _agents = agentDocs.Select(x => new AgentRecord
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                Description = x.Description,
                Instruction = x.Instruction,
                Functions = x.Functions,
                Responses = x.Responses,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _agents.AsQueryable();
        }
    }

    private List<UserRecord> _users;
    public IQueryable<UserRecord> User
    {
        get
        {
            if (_users != null)
            {
                return _users.AsQueryable();
            }

            var userDocs = _dc.Users?.AsQueryable()?.ToList() ?? new List<UserCollection>();
            _users = userDocs.Select(x => new UserRecord
            {
                Id = x.Id.ToString(),
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Password = x.Password,
                Salt = x.Salt,
                ExternalId = x.ExternalId,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _users.AsQueryable();
        }
    }

    private List<UserAgentRecord> _userAgents;
    public IQueryable<UserAgentRecord> UserAgent
    {
        get
        {
            if (_userAgents != null && _userAgents.Count > 0)
            {
                return _userAgents.AsQueryable();
            }

            var userDocs = _dc.UserAgents?.AsQueryable()?.ToList() ?? new List<UserAgentCollection>();
            _userAgents = userDocs.Select(x => new UserAgentRecord
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                UserId = x.UserId.ToString(),
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _userAgents.AsQueryable();
        }
    }

    private List<ConversationRecord> _conversations;
    public IQueryable<ConversationRecord> Conversation
    {
        get
        {
            if (_conversations != null)
            {
                return _conversations.AsQueryable();
            }

            var conversationDocs = _dc.Conversations?.AsQueryable()?.ToList() ?? new List<ConversationCollection>();
            _conversations = conversationDocs.Select(x => new ConversationRecord
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                UserId = x.UserId.ToString(),
                Title = x.Title,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _conversations.AsQueryable();
        }
    }

    private List<RoutingItemRecord> _routingItems;
    public IQueryable<RoutingItemRecord> RoutingItem
    {
        get
        {
            if (_routingItems != null)
            {
                return _routingItems.AsQueryable();
            }

            var routingItemDocs = _dc.RoutingItems?.AsQueryable()?.ToList() ?? new List<RoutingItemCollection>();
            _routingItems = routingItemDocs.Select(x => new RoutingItemRecord
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                Description = x.Description,
                RequiredFields = x.RequiredFields,
                RedirectTo = x.RedirectTo.ToString(),
                Disabled = x.Disabled
            }).ToList();

            return _routingItems.AsQueryable();
        }
    }

    private List<RoutingProfileRecord> _routingProfiles;
    public IQueryable<RoutingProfileRecord> RoutingProfile
    {
        get
        {
            if (_routingProfiles != null)
            {
                return _routingProfiles.AsQueryable();
            }

            var routingProfilDocs = _dc.RoutingProfiles?.AsQueryable()?.ToList() ?? new List<RoutingProfileCollection>();
            _routingProfiles = routingProfilDocs.Select(x => new RoutingProfileRecord
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                AgentIds = x.AgentIds?.Select(x => x.ToString())?.ToList() ?? new List<string>()
            }).ToList();

            return _routingProfiles.AsQueryable();
        }
    }


    List<string> _changedTableNames = new List<string>();
    public void Add<TTableInterface>(object entity)
    {
        if (entity is ConversationRecord conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(ConversationRecord));
        }
        else if (entity is AgentRecord agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(AgentRecord));
        }
        else if (entity is UserRecord user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(UserRecord));
        }
        else if (entity is UserAgentRecord userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgentRecord));
        }
    }

    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        foreach (var table in _changedTableNames)
        {
            if (table == nameof(ConversationRecord))
            {
                var conversations = _conversations.Select(x => new ConversationCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    AgentId = Guid.Parse(x.AgentId),
                    UserId = Guid.Parse(x.UserId),
                    Title = x.Title,
                    Dialog = x.Dialog,
                    State = x.State,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var conversation in conversations)
                {
                    var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversation.Id);
                    var update = Builders<ConversationCollection>.Update
                        .Set(x => x.AgentId, conversation.AgentId)
                        .Set(x => x.UserId, conversation.UserId)
                        .Set(x => x.Title, conversation.Title)
                        .Set(x => x.Dialog, conversation.Dialog)
                        .Set(x => x.State, conversation.State)
                        .Set(x => x.CreatedTime, conversation.CreatedTime)
                        .Set(x => x.UpdatedTime, conversation.UpdatedTime);
                    _dc.Conversations.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(AgentRecord))
            {
                var agents = _agents.Select(x => new AgentCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    Name = x.Name,
                    Description = x.Description,
                    Instruction = x.Instruction,
                    Functions = x.Functions,
                    Responses = x.Responses,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var agent in agents)
                {
                    var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agent.Id);
                    var update = Builders<AgentCollection>.Update
                        .Set(x => x.Name, agent.Name)
                        .Set(x => x.Description, agent.Description)
                        .Set(x => x.Instruction, agent.Instruction)
                        .Set(x => x.Functions, agent.Functions)
                        .Set(x => x.Responses, agent.Responses)
                        .Set(x => x.CreatedTime, agent.CreatedTime)
                        .Set(x => x.UpdatedTime, agent.UpdatedTime);
                    _dc.Agents.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(UserRecord))
            {
                var users = _users.Select(x => new UserCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Salt = x.Salt,
                    Password = x.Password,
                    Email = x.Email,
                    ExternalId = x.ExternalId,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var user in users)
                {
                    var filter = Builders<UserCollection>.Filter.Eq(x => x.Id, user.Id);
                    var update = Builders<UserCollection>.Update
                        .Set(x => x.FirstName, user.FirstName)
                        .Set(x => x.LastName, user.LastName)
                        .Set(x => x.Email, user.Email)
                        .Set(x => x.Salt, user.Salt)
                        .Set(x => x.Password, user.Password)
                        .Set(x => x.ExternalId, user.ExternalId)
                        .Set(x => x.CreatedTime, user.CreatedTime)
                        .Set(x => x.UpdatedTime, user.UpdatedTime);
                    _dc.Users.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(UserAgentRecord))
            {
                var userAgents = _userAgents.Select(x => new UserAgentCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    AgentId = Guid.Parse(x.AgentId),
                    UserId = Guid.Parse(x.UserId),
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var userAgent in userAgents)
                {
                    var filter = Builders<UserAgentCollection>.Filter.Eq(x => x.Id, userAgent.Id);
                    var update = Builders<UserAgentCollection>.Update
                        .Set(x => x.AgentId, userAgent.AgentId)
                        .Set(x => x.UserId, userAgent.UserId)
                        .Set(x => x.CreatedTime, userAgent.CreatedTime)
                        .Set(x => x.UpdatedTime, userAgent.UpdatedTime);
                    _dc.UserAgents.UpdateOne(filter, update, _options);
                }
            }
        }

        return _changedTableNames.Count;
    }

    public UserRecord GetUserByEmail(string email)
    {
        var user = User.FirstOrDefault(x => x.Email == email);
        return user != null ? new UserRecord 
        {
            Id = user.Id.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            ExternalId = user.ExternalId,
            CreatedTime = user.CreatedTime,
            UpdatedTime = user.UpdatedTime
        } : null;
    }

    public void CreateUser(UserRecord user)
    {
        if (user == null) return;

        var userCollection = new UserCollection
        {
            Id = Guid.NewGuid(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            ExternalId = user.ExternalId,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }

    public void UpdateAgent(AgentRecord agent)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var agentCollection = new AgentCollection
        {
            Id = Guid.Parse(agent.Id),
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Responses = agent.Responses,
            UpdatedTime = DateTime.UtcNow
        };


        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, Guid.Parse(agent.Id));
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Name, agent.Name)
            .Set(x => x.Description, agent.Description)
            .Set(x => x.Instruction, agent.Instruction)
            .Set(x => x.Functions, agent.Functions)
            .Set(x => x.Responses, agent.Responses)
            .Set(x => x.UpdatedTime, agent.UpdatedTime);

        _dc.Agents.UpdateOne(filter, update, _options);
    }

    public void DeleteRoutingItems()
    {
        _dc.RoutingItems.DeleteMany(Builders<RoutingItemCollection>.Filter.Empty);
    }

    public void DeleteRoutingProfiles()
    {
        _dc.RoutingProfiles.DeleteMany(Builders<RoutingProfileCollection>.Filter.Empty);
    }

    public List<RoutingItemRecord> CreateRoutingItems(List<RoutingItemRecord> routingItems)
    {
        var collections = routingItems?.Select(x => new RoutingItemCollection
        {
            Id = Guid.NewGuid(),
            AgentId = Guid.Parse(x.AgentId),
            Name = x.Name,
            Description = x.Description,
            RequiredFields = x.RequiredFields,
            RedirectTo = !string.IsNullOrEmpty(x.RedirectTo) ? Guid.Parse(x.RedirectTo) : null,
            Disabled = x.Disabled
        })?.ToList() ?? new List<RoutingItemCollection>();
        
        _dc.RoutingItems.InsertMany(collections);
        return collections.Select(x => new RoutingItemRecord
        {
            Id = x.Id.ToString(),
            AgentId = x.AgentId.ToString(),
            Name = x.Name,
            Description = x.Description,
            RequiredFields = x.RequiredFields,
            RedirectTo = x.RedirectTo?.ToString(),
            Disabled = x.Disabled
        }).ToList();
    }

    public List<RoutingProfileRecord> CreateRoutingProfiles(List<RoutingProfileRecord> profiles)
    {
        var collections = profiles?.Select(x => new RoutingProfileCollection
        {
            Id = Guid.NewGuid(),
            Name = x.Name,
            AgentIds = x.AgentIds.Select(x => Guid.Parse(x)).ToList()
        })?.ToList() ?? new List<RoutingProfileCollection>();

        _dc.RoutingProfiles.InsertMany(collections);
        return collections.Select(x => new RoutingProfileRecord
        {
            Id = x.Id.ToString(),
            Name = x.Name,
            AgentIds = x.AgentIds.Select(x => x.ToString()).ToList()
        }).ToList();
    }

    public List<string> GetAgentResponses(string agentId)
    {
        var responses = new List<string>();
        var agent = Agent.FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return responses;

        return agent.Responses;
    }

    public AgentRecord GetAgent(string agentId)
    {
        var foundAgent = Agent.FirstOrDefault(x => x.Id == agentId);
        return foundAgent;
    }

    public void CreateNewConversation(ConversationRecord conversation)
    {
        if (conversation == null) return;

        var collection = new ConversationCollection
        {
            Id = Guid.Parse(conversation.Id),
            AgentId = Guid.Parse(conversation.AgentId),
            UserId = Guid.Parse(conversation.UserId),
            Title = conversation.Title,
            Dialog = string.Empty,
            State = new List<KeyValueModel>(),
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow,
        };

        _dc.Conversations.InsertOne(collection);
    }

    public string GetConversationDialog(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return string.Empty;

        var filterById = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConversation = _dc.Conversations.Find(filterById).FirstOrDefault();
        if (foundConversation == null) return string.Empty;

        return foundConversation.Dialog;
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterById = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConversation = _dc.Conversations.Find(filterById).FirstOrDefault();
        if (foundConversation == null) return;

        var update = Builders<ConversationCollection>.Update
            .Set(x => x.Dialog, dialogs)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Conversations.UpdateOne(filterById, update);
    }

    public List<KeyValueModel> GetConversationState(string conversationId)
    {
        var states = new List<KeyValueModel>();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var filterById = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConversation = _dc.Conversations.Find(filterById).FirstOrDefault();
        if (foundConversation == null) return states;

        var savedStates = foundConversation.State ?? new List<KeyValueModel>();
        return savedStates;
    }

    public void UpdateConversationState(string conversationId, List<KeyValueModel> state)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterById = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConversation = _dc.Conversations.Find(filterById).FirstOrDefault();
        if (foundConversation == null) return;

        var update = Builders<ConversationCollection>.Update
            .Set(x => x.State, state)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Conversations.UpdateOne(filterById, update);
    }

    public ConversationRecord GetConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var filterById = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var found = _dc.Conversations.Find(filterById).FirstOrDefault();

        return found != null ? new ConversationRecord 
        { 
            Id = found.Id.ToString(),
            AgentId = found.AgentId.ToString(),
            UserId = found.UserId.ToString(),
            Title = found.Title,
            Dialog = found.Dialog,
            State = found.State,
            CreatedTime = found.CreatedTime,
            UpdatedTime = found.UpdatedTime
        }: null;
    }

    public List<ConversationRecord> GetConversations(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<ConversationRecord>();

        var filterByUserId = Builders<ConversationCollection>.Filter.Eq(x => x.UserId, Guid.Parse(userId));
        var conversations = _dc.Conversations.Find(filterByUserId).ToList();
        return conversations.Select(x => new ConversationRecord
        {
            Id = x.Id.ToString(),
            AgentId = x.AgentId.ToString(),
            UserId = x.UserId.ToString(),
            Title = x.Title,
            Dialog = x.Dialog,
            State = x.State,
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();
    }
}
