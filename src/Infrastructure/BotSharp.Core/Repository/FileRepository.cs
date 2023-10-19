using BotSharp.Abstraction.Repositories;
using System.IO;
using FunctionDef = BotSharp.Abstraction.Functions.Models.FunctionDef;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Agents.Models;
using MongoDB.Driver;
using BotSharp.Abstraction.Routing.Models;
namespace BotSharp.Core.Repository;

public class FileRepository : IBotSharpRepository
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly ConversationSetting _conversationSettings;
    private JsonSerializerOptions _options;

    public FileRepository(
        BotSharpDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        ConversationSetting conversationSettings)
    {
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _conversationSettings = conversationSettings;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    private List<User> _users = new List<User>();
    public IQueryable<User> Users
    {
        get
        {
            if (!_users.IsNullOrEmpty())
            {
                return _users.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _users = new List<User>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var json = File.ReadAllText(Path.Combine(d, "user.json"));
                    _users.Add(JsonSerializer.Deserialize<User>(json, _options));
                }
            }
            return _users.AsQueryable();
        }
    }

    private List<Agent> _agents = new List<Agent>();
    public IQueryable<Agent> Agents
    {
        get
        {
            if (!_agents.IsNullOrEmpty())
            {
                return _agents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            _agents = new List<Agent>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var json = File.ReadAllText(Path.Combine(d, "agent.json"));
                    var agent = JsonSerializer.Deserialize<Agent>(json, _options);
                    if (agent != null)
                    {
                        agent = agent.SetInstruction(FetchInstruction(d))
                                     .SetTemplates(FetchTemplates(d))
                                     .SetFunctions(FetchFunctions(d))
                                     .SetResponses(FetchResponses(d));
                        _agents.Add(agent);
                    }
                }
            }
            return _agents.AsQueryable();
        }
    }

    private List<UserAgent> _userAgents = new List<UserAgent>();
    public IQueryable<UserAgent> UserAgents
    {
        get
        {
            if (!_userAgents.IsNullOrEmpty())
            {
                return _userAgents.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, "users");
            _userAgents = new List<UserAgent>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var file = Path.Combine(d, "agents.json");
                    if (Directory.Exists(d) && File.Exists(file))
                    {
                        var json = File.ReadAllText(file);
                        _userAgents.AddRange(JsonSerializer.Deserialize<List<UserAgent>>(json, _options));
                    }
                }
            }
            return _userAgents.AsQueryable();
        }
    }

    private List<Conversation> _conversations = new List<Conversation>();
    public IQueryable<Conversation> Conversations
    {
        get
        {
            if (!_conversations.IsNullOrEmpty())
            {
                return _conversations.AsQueryable();
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);
            _conversations = new List<Conversation>();
            if (Directory.Exists(dir))
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    var path = Path.Combine(d, "conversation.json");
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        _conversations.Add(JsonSerializer.Deserialize<Conversation>(json, _options));
                    }
                }
            }
            return _conversations.AsQueryable();
        }
    }

    public void Add<TTableInterface>(object entity)
    {
        if (entity is Conversation conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(Conversation));
        }
        else if (entity is Agent agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(Agent));
        }
        else if (entity is User user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(User));
        }
        else if (entity is UserAgent userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgent));
        }
    }

    List<string> _changedTableNames = new List<string>();
    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        // Persist to disk
        foreach (var table in _changedTableNames)
        {
            if (table == nameof(Conversation))
            {
                foreach (var conversation in _conversations)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversation.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "conversation.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(conversation, _options));
                }
            }
            else if (table == nameof(Agent))
            {
                foreach (var agent in _agents)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agent.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "agent.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(agent, _options));
                }
            }
            else if (table == nameof(User))
            {
                foreach (var user in _users)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository, "users", user.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, "user.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
                }
            }
            else if (table == nameof(UserAgent))
            {
                _userAgents.GroupBy(x => x.UserId)
                    .Select(x => x.Key).ToList()
                    .ForEach(uid =>
                    {
                        var agents = _userAgents.Where(x => x.UserId == uid).ToList();
                        if (agents.Any())
                        {
                            var dir = Path.Combine(_dbSettings.FileRepository, "users", uid);
                            var path = Path.Combine(dir, "agents.json");
                            File.WriteAllText(path, JsonSerializer.Serialize(agents, _options));
                        }
                    });
            }
        }

        return _changedTableNames.Count;
    }


    #region Agent
    public void UpdateAgent(Agent agent, AgentField field)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        switch (field)
        {
            case AgentField.Name:
                UpdateAgentName(agent.Id, agent.Name);
                break;
            case AgentField.Description:
                UpdateAgentDescription(agent.Id, agent.Description);
                break;
            case AgentField.IsPublic:
                UpdateAgentIsPublic(agent.Id, agent.IsPublic);
                break;
            case AgentField.Disabled:
                UpdateAgentDisabled(agent.Id, agent.Disabled);
                break;
            case AgentField.AllowRouting:
                UpdateAgentAllowRouting(agent.Id, agent.AllowRouting);
                break;
            case AgentField.Profiles:
                UpdateAgentProfiles(agent.Id, agent.Profiles);
                break;
            case AgentField.RoutingRules:
                UpdateAgentRoutingRules(agent.Id, agent.RoutingRules);
                break;
            case AgentField.Instruction:
                UpdateAgentInstruction(agent.Id, agent.Instruction);
                break;
            case AgentField.Function:
                UpdateAgentFunctions(agent.Id, agent.Functions);
                break;
            case AgentField.Template:
                UpdateAgentTemplates(agent.Id, agent.Templates);
                break;
            case AgentField.Response:
                UpdateAgentResponses(agent.Id, agent.Responses);
                break;
            case AgentField.All:
                UpdateAgentAllFields(agent);
                break;
            default:
                break;
        }
    }

    #region Update Agent Fields
    private void UpdateAgentName(string agentId, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.Name = name;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentDescription(string agentId, string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.Description = description;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentIsPublic(string agentId, bool isPublic)
    {
        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.IsPublic = isPublic;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentDisabled(string agentId, bool disabled)
    {
        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.Disabled = disabled;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentAllowRouting(string agentId, bool allowRouting)
    {
        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.AllowRouting = allowRouting;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentProfiles(string agentId, List<string> profiles)
    {
        if (profiles.IsNullOrEmpty()) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.Profiles = profiles;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
    {
        if (rules.IsNullOrEmpty()) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        agent.RoutingRules = rules;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);
    }

    private void UpdateAgentInstruction(string agentId, string instruction)
    {
        if (string.IsNullOrEmpty(instruction)) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        var instructionFile = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir,
                                        agentId, $"instruction.{_agentSettings.TemplateFormat}");

        File.WriteAllText(instructionFile, instruction);
    }

    private void UpdateAgentFunctions(string agentId, List<FunctionDef> inputFunctions)
    {
        if (inputFunctions.IsNullOrEmpty()) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        var functionFile = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir,
                                        agentId, "functions.json");

        var functions = new List<string>();
        foreach (var function in inputFunctions)
        {
            functions.Add(JsonSerializer.Serialize(function, _options));
        }

        var functionText = JsonSerializer.Serialize(functions, _options);
        File.WriteAllText(functionFile, functionText);
    }

    private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
    {
        if (templates.IsNullOrEmpty()) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        var templateDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "templates");

        if (!Directory.Exists(templateDir))
        {
            Directory.CreateDirectory(templateDir);
        }

        foreach (var file in Directory.GetFiles(templateDir))
        {
            File.Delete(file);
        }

        foreach (var template in templates)
        {
            var file = Path.Combine(templateDir, $"{template.Name}.{_agentSettings.TemplateFormat}");
            File.WriteAllText(file, template.Content);
        }
    }

    private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
    {
        if (responses.IsNullOrEmpty()) return;

        var (agent, agentFile) = GetAgentFromFile(agentId);
        if (agent == null) return;

        var responseDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "responses");
        if (!Directory.Exists(responseDir))
        {
            Directory.CreateDirectory(responseDir);
        }

        foreach (var file in Directory.GetFiles(responseDir))
        {
            File.Delete(file);
        }

        for (int i = 0; i < responses.Count; i++)
        {
            var response = responses[i];
            var fileName = $"{response.Prefix}.{response.Intent}.{i}.{_agentSettings.TemplateFormat}";
            var file = Path.Combine(responseDir, fileName);
            File.WriteAllText(file, response.Content);
        }
    }

    private void UpdateAgentAllFields(Agent inputAgent)
    {
        var (agent, agentFile) = GetAgentFromFile(inputAgent.Id);
        if (agent == null) return;

        agent.Name = inputAgent.Name;
        agent.Description = inputAgent.Description;
        agent.IsPublic = inputAgent.IsPublic;
        agent.Disabled = inputAgent.Disabled;
        agent.AllowRouting = inputAgent.AllowRouting;
        agent.Profiles = inputAgent.Profiles;
        agent.RoutingRules = inputAgent.RoutingRules;
        agent.UpdatedDateTime = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(agent, _options);
        File.WriteAllText(agentFile, json);

        UpdateAgentInstruction(inputAgent.Id, inputAgent.Instruction);
        UpdateAgentResponses(inputAgent.Id, inputAgent.Responses);
        UpdateAgentTemplates(inputAgent.Id, inputAgent.Templates);
        UpdateAgentFunctions(inputAgent.Id, inputAgent.Functions);
    }
    #endregion

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        var responses = new List<string>();
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "responses");
        if (!Directory.Exists(dir)) return responses;

        foreach (var file in Directory.GetFiles(dir))
        {
            if (file.Split(Path.DirectorySeparatorChar)
                .Last()
                .StartsWith(prefix + "." + intent))
            {
                responses.Add(File.ReadAllText(file));
            }
        }

        return responses;
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public Agent? GetAgent(string agentId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
        var dir = Directory.GetDirectories(agentDir).FirstOrDefault(x => x.Split(Path.DirectorySeparatorChar).Last() == agentId);

        if (!string.IsNullOrEmpty(dir))
        {
            var json = File.ReadAllText(Path.Combine(dir, "agent.json"));
            if (string.IsNullOrEmpty(json)) return null;

            var record = JsonSerializer.Deserialize<Agent>(json, _options);
            if (record == null) return null;

            var instruction = FetchInstruction(dir);
            var functions = FetchFunctions(dir);
            var samples = FetchSamples(dir);
            var templates = FetchTemplates(dir);
            var responses = FetchResponses(dir);
            return record.SetInstruction(instruction)
                             .SetFunctions(functions)
                             .SetSamples(samples)
                             .SetTemplates(templates)
                             .SetResponses(responses);
        }

        return null;
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, "templates");
        if (!Directory.Exists(dir)) return string.Empty;

        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = splits[0];
            var extension = splits[1];
            if (name.IsEqualTo(templateName) && extension.IsEqualTo(_agentSettings.TemplateFormat))
            {
                return File.ReadAllText(file);
            }
        }

        return string.Empty;
    }

    public void BulkInsertAgents(List<Agent> agents)
    {
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
    }

    public bool DeleteAgents()
    {
        return false;
    }
    #endregion

    #region Conversation
    public void CreateNewConversation(Conversation conversation)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversation.Id);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var convDir = Path.Combine(dir, "conversation.json");
        if (!File.Exists(convDir))
        {
            File.WriteAllText(convDir, JsonSerializer.Serialize(conversation, _options));
        }

        var dialogDir = Path.Combine(dir, "dialogs.txt");
        if (!File.Exists(dialogDir))
        {
            File.WriteAllText(dialogDir, string.Empty);
        }

        var stateDir = Path.Combine(dir, "state.dict");
        if (!File.Exists(stateDir))
        {
            File.WriteAllText(stateDir, string.Empty);
        }
    }

    public string GetConversationDialog(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogDir = Path.Combine(convDir, "dialogs.txt");
            if (File.Exists(dialogDir))
            {
                return File.ReadAllText(dialogDir);
            }
        }

        return string.Empty;
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogDir = Path.Combine(convDir, "dialogs.txt");
            if (File.Exists(dialogDir))
            {
                File.WriteAllText(dialogDir, dialogs);
            }
        }

        return;
    }

    public List<StateKeyValue> GetConversationStates(string conversationId)
    {
        var curStates = new List<StateKeyValue>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateDir = Path.Combine(convDir, "state.dict");
            if (File.Exists(stateDir))
            {
                var dict = File.ReadAllLines(stateDir);
                foreach (var line in dict)
                {
                    var data = line.Split('=');
                    curStates.Add(new StateKeyValue(data[0], data[1]));
                }
            }
        }

        return curStates;
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        var localStates = new List<string>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateDir = Path.Combine(convDir, "state.dict");
            if (File.Exists(stateDir))
            {
                foreach (var data in states)
                {
                    localStates.Add($"{data.Key}={data.Value}");
                }
                File.WriteAllLines(stateDir, localStates);
            }
        }
    }

    public Conversation GetConversation(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, "conversation.json");
            var content = File.ReadAllText(convFile);
            var record = JsonSerializer.Deserialize<Conversation>(content, _options);

            var dialogFile = Path.Combine(convDir, "dialogs.txt");
            if (record != null && File.Exists(dialogFile))
            {
                record.Dialog = File.ReadAllText(dialogFile);
            }

            var stateFile = Path.Combine(convDir, "state.dict");
            if (record != null && File.Exists(stateFile))
            {
                var states = File.ReadLines(stateFile);
                record.States = new ConversationState(states.Select(x => new StateKeyValue(x.Split('=')[0], x.Split('=')[1])).ToList());
            }

            return record;
        }

        return null;
    }

    public List<Conversation> GetConversations(string userId)
    {
        var records = new List<Conversation>();
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var record = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (record != null && record.UserId == userId)
            {
                records.Add(record);
            }
        }

        return records;
    }
    #endregion

    #region User
    public User? GetUserByEmail(string email)
    {
        return Users.FirstOrDefault(x => x.Email == email);
    }

    public void CreateUser(User user)
    {
        var userId = Guid.NewGuid().ToString();
        var dir = Path.Combine(_dbSettings.FileRepository, "users", userId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var path = Path.Combine(dir, "user.json");
        File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
    }
    #endregion

    #region Private methods
    private string GetAgentDataDir(string agentId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(dir))
        {
            dir = string.Empty;
        }
        return dir;
    }

    private (Agent?, string) GetAgentFromFile(string agentId)
    {
        var dir = GetAgentDataDir(agentId);
        var agentFile = Path.Combine(dir, "agent.json");
        if (!File.Exists(agentFile)) return (null, string.Empty);

        var json = File.ReadAllText(agentFile);
        var agent = JsonSerializer.Deserialize<Agent>(json, _options);
        return (agent, agentFile);
    }

    private string FetchInstruction(string fileDir)
    {
        var file = Path.Combine(fileDir, $"instruction.{_agentSettings.TemplateFormat}");
        if (!File.Exists(file)) return string.Empty;

        var instruction = File.ReadAllText(file);
        return instruction;
    }

    private List<FunctionDef> FetchFunctions(string fileDir)
    {
        var file = Path.Combine(fileDir, "functions.json");
        if (!File.Exists(file)) return new List<FunctionDef>();

        var functionsJson = File.ReadAllText(file);
        var functions = JsonSerializer.Deserialize<List<FunctionDef>>(functionsJson, _options);
        return functions;
    }

    private List<string> FetchSamples(string fileDir)
    {
        var file = Path.Combine(fileDir, "samples.txt");
        if (!File.Exists(file)) return new List<string>();

        return File.ReadAllLines(file).ToList();
    }

    private List<AgentTemplate> FetchTemplates(string fileDir)
    {
        var templates = new List<AgentTemplate>();
        var templateDir = Path.Combine(fileDir, "templates");
        if (!Directory.Exists(templateDir)) return templates;

        foreach (var file in Directory.GetFiles(templateDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var name = string.Join('.', splits.Take(splits.Length - 1));
            var extension = splits.Last();
            if (extension.Equals(_agentSettings.TemplateFormat, StringComparison.OrdinalIgnoreCase))
            {
                var content = File.ReadAllText(file);
                templates.Add(new AgentTemplate(name, content));
            }
        }

        return templates;
    }

    private List<AgentResponse> FetchResponses(string fileDir)
    {
        var responses = new List<AgentResponse>();
        var responseDir = Path.Combine(fileDir, "responses");
        if (!Directory.Exists(responseDir)) return responses;

        foreach (var file in Directory.GetFiles(responseDir))
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var splits = fileName.ToLower().Split('.');
            var prefix = splits[0];
            var intent = splits[1];
            var content = File.ReadAllText(file);
            responses.Add(new AgentResponse(prefix, intent, content));
        }

        return responses;
    }

    private string? FindConversationDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, "conversation.json");
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var conv = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (conv != null && conv.Id == conversationId)
            {
                return d;
            }
        }

        return null;
    }
    #endregion
}
