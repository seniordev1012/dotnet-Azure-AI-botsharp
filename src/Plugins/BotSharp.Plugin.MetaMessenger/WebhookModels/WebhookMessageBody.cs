using BotSharp.Plugin.MetaMessenger.MessagingModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookMessageBody
{
    [JsonPropertyName("mid")]
    public string Id { get;set; }
    public string Text { get;set; }
    [JsonPropertyName("quick_reply")]
    public QuickReplyMessageItem QuickReply { get;set; }
}
