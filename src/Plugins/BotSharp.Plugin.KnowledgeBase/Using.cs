global using System;
global using System.IO;
global using System.Collections.Generic;
global using System.Text;
global using System.Threading.Tasks;
global using System.Linq;
global using System.Text.Json;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using BotSharp.Abstraction.Plugins;
global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Conversations;
global using BotSharp.Abstraction.Knowledges;
global using BotSharp.Abstraction.Users;
global using BotSharp.Abstraction.Utilities;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Agents.Settings;
global using BotSharp.Abstraction.Graph;
global using BotSharp.Abstraction.Knowledges.Settings;
global using BotSharp.Abstraction.Knowledges.Enums;
global using BotSharp.Abstraction.VectorStorage;
global using BotSharp.Abstraction.Knowledges.Models;
global using BotSharp.Abstraction.MLTasks;
global using BotSharp.Abstraction.Functions;
global using BotSharp.Abstraction.Messaging.Enums;
global using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
global using BotSharp.Abstraction.Messaging.Models.RichContent;
global using BotSharp.Abstraction.Messaging;
global using BotSharp.Abstraction.Agents.Enums;
global using BotSharp.Abstraction.Agents.Models;
global using BotSharp.Abstraction.Functions.Models;
global using BotSharp.Abstraction.Repositories;
global using BotSharp.Plugin.KnowledgeBase.Services;
global using BotSharp.Plugin.KnowledgeBase.Enum;