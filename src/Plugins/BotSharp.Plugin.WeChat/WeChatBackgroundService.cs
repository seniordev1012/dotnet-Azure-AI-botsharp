using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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
            var conversationService = scoped.GetRequiredService<IConversationService>();

            var result = await conversationService.SendMessage(openid, Guid.Empty.ToString(), new RoleDialogModel
            {
                Role = "user",
                Text = message,
            });

            await ReplyTextMessageAsync(openid, result);
        }

        private async Task ReplyTextMessageAsync(string openid, string content)
        {
            var appId = Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;
            await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(appId, openid, content);
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
