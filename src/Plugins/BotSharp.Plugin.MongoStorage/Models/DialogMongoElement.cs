using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class DialogMongoElement
{
    public DialogMetaDataMongoElement MetaData { get; set; }
    public string Content { get; set; }
    public string? RichContent { get; set; }

    public DialogMongoElement()
    {

    }

    public static DialogMongoElement ToMongoElement(DialogElement dialog)
    {
        return new DialogMongoElement
        {
            MetaData = DialogMetaDataMongoElement.ToMongoElement(dialog.MetaData),
            Content = dialog.Content,
            RichContent = dialog.RichContent
        };
    }

    public static DialogElement ToDomainElement(DialogMongoElement dialog)
    {
        return new DialogElement
        {
            MetaData = DialogMetaDataMongoElement.ToDomainElement(dialog.MetaData),
            Content = dialog.Content,
            RichContent = dialog.RichContent
        };
    }
}

public class DialogMetaDataMongoElement
{
    public string Role { get; set; }
    public string AgentId { get; set; }
    public string MessageId { get; set; }
    public string? FunctionName { get; set; }
    public string? SenderId { get; set; }
    public DateTime CreateTime { get; set; }

    public DialogMetaDataMongoElement()
    {

    }

    public static DialogMetaData ToDomainElement(DialogMetaDataMongoElement meta)
    {
        return new DialogMetaData
        {
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreateTime,
        };
    }

    public static DialogMetaDataMongoElement ToMongoElement(DialogMetaData meta)
    {
        return new DialogMetaDataMongoElement
        { 
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreateTime,
        };
    }
}
