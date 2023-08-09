using BotSharp.Abstraction.Conversations.Models;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

/// <summary>
/// Maintain the conversation state
/// </summary>
public class ConversationStateService : IConversationStateService, IDisposable
{
    private ConversationState _state;
    private IAgentService _agentService;
    private string _conversationId;
    private string _file;

    public ConversationStateService(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public void SetState(string name, string value)
    {
        _state[name] = value;
    }

    public void Dispose()
    {
        Save();
    }

    public ConversationState Load(string conversationId)
    {
        if (_state != null)
        {
            return _state;
        }

        _state = new ConversationState();
        _conversationId = conversationId;

        _file = GetStorageFile(_conversationId);

        if (File.Exists(_file))
        {
            var dict = File.ReadAllLines(_file);
            foreach (var line in dict)
            {
                _state[line.Split(':')[0]] = line.Split(':')[1];
            }
        }

        return _state;
    }

    public void Save()
    {
        var states = new List<string>();
        
        foreach (var dic in _state)
        {
            states.Add($"{dic.Key}:{dic.Value}");
        }
        File.WriteAllLines(_file, states);
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = _agentService.GetDataDir();
        return Path.Combine(dir, "conversations", conversationId + ".state");
    }

    public string GetState(string name)
    {
        if (!_state.ContainsKey(name))
        {
            _state[name] = "";
        }
        return _state[name];
    }
}
