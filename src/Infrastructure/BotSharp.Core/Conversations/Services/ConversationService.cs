using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ConversationSetting _settings;
    private readonly IConversationStorage _storage;
    private readonly IConversationStateService _state;
    private string _conversationId;
    public string ConversationId => _conversationId;

    public IConversationStateService States => _state;

    public ConversationService(
        IServiceProvider services,
        IUserIdentity user,
        ConversationSetting settings,
        IConversationStorage storage,
        IConversationStateService state,
        ILogger<ConversationService> logger)
    {
        _services = services;
        _user = user;
        _settings = settings;
        _storage = storage;
        _state = state;
        _logger = logger;
    }

    public async Task<bool> DeleteConversation(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var isDeleted = db.DeleteConversation(id);
        return await Task.FromResult(isDeleted);
    }

    public async Task<Conversation> UpdateConversationTitle(string id, string title)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateConversationTitle(id, title);
        var conversation = db.GetConversation(id);
        return conversation;
    }
    public async Task<Conversation> GetConversation(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var conversation = db.GetConversation(id);
        return conversation;
    }

    public async Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var conversations = db.GetConversations(filter);
        var result = new PagedItems<Conversation>
        {
            Count = conversations.Count(),
            Items = conversations.OrderByDescending(x => x.CreatedTime)
        };
        return result;
    }

    public async Task<List<Conversation>> GetLastConversations()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        return db.GetLastConversations();
    }

    public async Task<Conversation> NewConversation(Conversation sess)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var user = db.GetUserById(_user.Id);
        var foundUserId = user?.Id ?? string.Empty;

        var record = sess;
        record.Id = sess.Id.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        record.UserId = sess.UserId.IfNullOrEmptyAs(foundUserId);
        record.Title = "New Conversation";

        db.CreateNewConversation(record);

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            // If user connect agent first time
            await hook.OnUserAgentConnectedInitially(sess);

            await hook.OnConversationInitialized(record);
        }

        return record;
    }

    public Task CleanHistory(string agentId)
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory(int lastCount = 50)
    {
        if (string.IsNullOrEmpty(_conversationId))
        {
            throw new ArgumentNullException("ConversationId is null.");
        }

        var dialogs = _storage.GetDialogs(_conversationId);
        return dialogs
            .TakeLast(lastCount)
            .ToList();
    }

    public void SetConversationId(string conversationId, List<string> states)
    {
        _conversationId = conversationId;
        _state.Load(_conversationId);
        states.ForEach(x => _state.SetState(x.Split('=')[0], x.Split('=')[1]));
    }
}
