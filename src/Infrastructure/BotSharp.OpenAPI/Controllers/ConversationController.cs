namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public ConversationController(IServiceProvider services,
        IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost("/conversation/{agentId}")]
    public async Task<ConversationViewModel> NewConversation([FromRoute] string agentId, [FromBody] MessageConfig config)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conv = new Conversation
        {
            AgentId = agentId,
            Channel = ConversationChannel.OpenAPI,
            UserId = _user.Id
        };
        conv = await service.NewConversation(conv);
        service.SetConversationId(conv.Id, config.States);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpGet("/conversations")]
    public async Task<PagedItems<ConversationViewModel>> GetConversations([FromQuery] ConversationFilter filter)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conversations = await service.GetConversations(filter);

        var userService = _services.GetRequiredService<IUserService>();
        var list = conversations.Items
            .Select(x => ConversationViewModel.FromSession(x))
            .ToList();

        foreach (var item in list)
        {
            var user = await userService.GetUser(item.User.Id);
            item.User = UserViewModel.FromUser(user);
        }

        return new PagedItems<ConversationViewModel>
        {
            Count = conversations.Count,
            Items = list
        };
    }

    [HttpGet("/conversation/{conversationId}/dialogs")]
    public async Task<IEnumerable<ChatResponseModel>> GetDialogs([FromRoute] string conversationId)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, new List<string>());
        var history = conv.GetDialogHistory();

        var userService = _services.GetRequiredService<IUserService>();

        var dialogs = new List<ChatResponseModel>();
        foreach (var message in history)
        {
            var user = await userService.GetUser(message.SenderId);

            dialogs.Add(new ChatResponseModel
            {
                ConversationId = conversationId,
                MessageId = message.MessageId,
                CreatedAt = message.CreatedAt,
                Text = message.Content,
                Sender = UserViewModel.FromUser(user)
            });
        }

        return dialogs;
    }

    [HttpDelete("/conversation/{conversationId}")]
    public async Task<bool> DeleteConversation([FromRoute] string conversationId)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var response = await conversationService.DeleteConversation(conversationId);
        return response;
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<ChatResponseModel> SendMessage([FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, input.States);
        conv.States.SetState("channel", input.Channel)
                   .SetState("provider", input.Provider)
                   .SetState("model", input.Model)
                   .SetState("temperature", input.Temperature)
                   .SetState("sampling_factor", input.SamplingFactor);

        var response = new ChatResponseModel();
        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text);
        await conv.SendMessage(agentId, inputMsg,
            async msg =>
            {
                response.Text = msg.Content;
                response.Function = msg.FunctionName;
                response.RichContent = msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            },
            async fnExecuting =>
            {

            },
            async fnExecuted =>
            {

            });

        var state = _services.GetRequiredService<IConversationStateService>();
        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;
        response.ConversationId = conversationId;

        return response;
    }

    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId, 
        IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var attachmentService = _services.GetRequiredService<IConversationAttachmentService>();
            var dir = attachmentService.GetDirectory(conversationId);
            foreach (var file in files)
            {
                // Save the file, process it, etc.
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var filePath = Path.Combine(dir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
            }

            return Ok(new { message = "File uploaded successfully." });
        }

        return BadRequest(new { message = "Invalid file." });
    }
}
