using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookRequest
{
    public string Object { get;set; }
    public List<WebhookObject> Entry { get;set; }
}
