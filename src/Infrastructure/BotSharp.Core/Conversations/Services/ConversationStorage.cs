using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Repositories;
using System;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly JsonSerializerOptions _options;

    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true,
            Converters =
            {
                new RichContentJsonConverter(),
                new TemplateMessageJsonConverter(),
            }
        };
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var agentId = dialog.CurrentAgentId;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogElements = new List<DialogElement>();

        if (dialog.Role == AgentRole.Function)
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                FunctionName = dialog.FunctionName,
                CreateTime = dialog.CreatedAt
            }; 
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            dialogElements.Add(new DialogElement(meta, content));
        }
        else
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                SenderId = dialog.SenderId,
                CreateTime = dialog.CreatedAt
            };
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            var richContent = dialog.RichContent != null ? JsonSerializer.Serialize(dialog.RichContent, _options) : null;
            dialogElements.Add(new DialogElement(meta, content, richContent));
        }

        db.AppendConversationDialogs(conversationId, dialogElements);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogs = db.GetConversationDialogs(conversationId);
        var hooks = _services.GetServices<IConversationHook>();

        var results = new List<RoleDialogModel>();
        foreach (var dialog in dialogs)
        {
            var meta = dialog.MetaData;
            var content = dialog.Content;
            var role = meta.Role;
            var currentAgentId = meta.AgentId;
            var messageId = meta.MessageId;
            var function = role == AgentRole.Function ? meta.FunctionName : null;
            var senderId = role == AgentRole.Function ? currentAgentId : meta.SenderId;
            var createdAt = meta.CreateTime;
            var richContent = !string.IsNullOrEmpty(dialog.RichContent) ? 
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.RichContent, _options) : null;

            var record = new RoleDialogModel(role, content)
            {
                CurrentAgentId = currentAgentId,
                MessageId = messageId,
                CreatedAt = createdAt,
                SenderId = senderId,
                FunctionName = function,
                RichContent = richContent
            };
            results.Add(record);

            foreach(var hook in hooks)
            {
                hook.OnDialogRecordLoaded(record).Wait();
            }
        }

        foreach (var hook in hooks)
        {
            hook.OnDialogsLoaded(results).Wait();
        }

        return results;
    }

    public void InitStorage(string conversationId)
    {
        var file = GetStorageFile(conversationId);
        if (!File.Exists(file))
        {
            File.WriteAllLines(file, new string[0]);
        }
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "dialogs.txt");
    }
}
