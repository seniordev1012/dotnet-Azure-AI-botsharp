global using System;
global using System.Collections.Generic;
global using System.Text;
global using System.Threading.Tasks;
global using System.Linq;
global using System.Text.Json;
global using System.Net.Http.Headers;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using BotSharp.Abstraction.Plugins;
global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Conversations;
global using BotSharp.Abstraction.Knowledges;
global using BotSharp.Abstraction.Users;
global using BotSharp.Abstraction.Users.Models;
global using BotSharp.Abstraction.Utilities;
global using BotSharp.Abstraction.Agents.Settings;
global using BotSharp.Abstraction.Conversations.Settings;
global using BotSharp.Abstraction.Agents.Enums;
global using BotSharp.Abstraction.ApiAdapters;
global using BotSharp.Abstraction.Conversations.Enums;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Models;
global using BotSharp.Abstraction.Repositories.Filters;
global using BotSharp.OpenAPI.ViewModels.Conversations;
global using BotSharp.OpenAPI.ViewModels.Users;
global using BotSharp.OpenAPI.ViewModels.Agents;