using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.WeChat.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BotSharp.Plugin.WeChat
{
    public class WeChatBackgroundService : BackgroundService, IMessageQueue
    {
        private readonly Channel<WeChatMessage> _queue;
        private readonly IServiceProvider _service;
        private readonly ILogger<WeChatBackgroundService> _logger;
        private string WeChatAppId => Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;
        public static string AgentId { get; set; }

        public WeChatBackgroundService(
            IServiceProvider service,
            ILogger<WeChatBackgroundService> logger)
        {

            _service = service;
            _logger = logger;
            _queue = Channel.CreateUnbounded<WeChatMessage>();
        }

        private async Task HandleTextMessageAsync(string openid, string message)
        {
            var scoped = _service.CreateScope().ServiceProvider;

            var user = await GetWeChatAccountUserAsync(openid, scoped);
            BindWeChatAccountUser(user, scoped);

            var conversationService = scoped.GetRequiredService<IConversationService>();

            var latestConversationId = (await conversationService.GetConversations(new ConversationFilter
            {
            })).Items
            .OrderByDescending(_ => _.CreatedTime)
            .FirstOrDefault()?.Id;

            var inputMsg = new RoleDialogModel(AgentRole.User, message);
            var routing = _service.GetRequiredService<IRoutingService>();
            routing.Context.SetMessageId(latestConversationId, inputMsg.MessageId);

            conversationService.SetConversationId(latestConversationId, new List<MessageState>
            {
                new MessageState("channel", "wechat")
            });

            latestConversationId ??= (await conversationService.NewConversation(new Conversation()
            {
                UserId = openid,
                AgentId = AgentId
            }))?.Id;

            var result = await conversationService.SendMessage(AgentId,
                inputMsg,
                replyMessage: null, 
                async msg =>
                {
                    await ReplyTextMessageAsync(openid, msg.Content);
                }, 
                _ => Task.CompletedTask, 
                _ => Task.CompletedTask);
        }

        private async Task<User> GetWeChatAccountUserAsync(string openId, IServiceProvider service)
        {
            var userService = service.GetRequiredService<IWeChatAccountUserService>();

            return await userService.GetOrCreateWeChatAccountUserAsync(WeChatAppId, openId);
        }
        private void BindWeChatAccountUser(User user,IServiceProvider service)
        {
            var context = service.GetService<IHttpContextAccessor>();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            context.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            };
        }

        private async Task ReplyTextMessageAsync(string openid, string content)
        {
            await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(WeChatAppId, openid, content);
        }

        public async Task EnqueueAsync(WeChatMessage message)
        {
            await _queue.Writer.WriteAsync(message);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.Reader.ReadAsync(cancellationToken);
                    await HandleTextMessageAsync(message.OpenId, message.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred Handle Message");
                }
            }
        }
    }
}
