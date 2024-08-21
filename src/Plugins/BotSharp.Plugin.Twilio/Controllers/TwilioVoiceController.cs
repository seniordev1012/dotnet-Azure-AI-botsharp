using BotSharp.Abstraction.Files;
using BotSharp.Core.Infrastructures;
using BotSharp.Plugin.Twilio.Models;
using BotSharp.Plugin.Twilio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace BotSharp.Plugin.Twilio.Controllers;

[AllowAnonymous]
[Route("twilio/voice")]
public class TwilioVoiceController : TwilioController
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;

    public TwilioVoiceController(TwilioSetting settings, IServiceProvider services)
    {
        _settings = settings;
        _services = services;
    }

    [Authorize]
    [HttpGet("/twilio/token")]
    public Token GetAccessToken()
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var accessToken = twilio.GetAccessToken();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return new Token
        {
            AccessToken = accessToken,
            ExpireTime = jwt.Payload.Exp.Value,
            TokenType = "Bearer",
            Scope = "api"
        };
    }

    [HttpPost("welcome")]
    public TwiMLResult InitiateConversation(VoiceRequest request, [FromQuery] string states)
    {
        if (request?.CallSid == null) throw new ArgumentNullException(nameof(VoiceRequest.CallSid));
        string conversationId = $"TwilioVoice_{request.CallSid}";
        var twilio = _services.GetRequiredService<TwilioService>();
        var url = $"twilio/voice/{conversationId}/receive/0?states={states}";
        var response = twilio.ReturnInstructions("twilio/welcome.mp3", url, true);
        return TwiML(response);
    }

    [HttpPost("{conversationId}/receive/{seqNum}")]
    public async Task<TwiMLResult> ReceiveCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum, [FromQuery] string states, VoiceRequest request)
    {
        var twilio = _services.GetRequiredService<TwilioService>();
        var messageQueue = _services.GetRequiredService<TwilioMessageQueue>();
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var url = $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}";
        var messages = await sessionManager.RetrieveStagedCallerMessagesAsync(conversationId, seqNum);
        if (!string.IsNullOrWhiteSpace(request.SpeechResult))
        {
            messages.Add(request.SpeechResult);
        }
        var messageContent = string.Join("\r\n", messages);
        VoiceResponse response;
        if (!string.IsNullOrWhiteSpace(messageContent))
        {

            var callerMessage = new CallerMessage()
            {
                ConversationId = conversationId,
                SeqNumber = seqNum,
                Content = messageContent,
                From = request.From
            };
            if (!string.IsNullOrEmpty(states))
            {
                var kvp = states.Split(':');
                if (kvp.Length == 2)
                {
                    callerMessage.States.Add(kvp[0], kvp[1]);
                }
            }
            await messageQueue.EnqueueAsync(callerMessage);
            response = twilio.ReturnInstructions(null, url, true, 1);
        }
        else
        {
            var speechPath = seqNum > 0 ? $"twilio/voice/speeches/{conversationId}/{seqNum - 1}.mp3" : "twilio/welcome.mp3";
            response = twilio.ReturnInstructions(speechPath, $"twilio/voice/{conversationId}/receive/{seqNum}?states={states}", true);
        }
        return TwiML(response);
    }

    [HttpPost("{conversationId}/reply/{seqNum}")]
    public async Task<TwiMLResult> ReplyCallerMessage([FromRoute] string conversationId, [FromRoute] int seqNum, [FromQuery] string states, VoiceRequest request)
    {
        var nextSeqNum = seqNum + 1;
        var sessionManager = _services.GetRequiredService<ITwilioSessionManager>();
        var twilio = _services.GetRequiredService<TwilioService>();
        if (request.SpeechResult != null)
        {
            await sessionManager.StageCallerMessageAsync(conversationId, nextSeqNum, request.SpeechResult);
        }
        var reply = await sessionManager.GetAssistantReplyAsync(conversationId, seqNum);
        VoiceResponse response;
        if (reply == null)
        {
            var indication = await sessionManager.GetReplyIndicationAsync(conversationId, seqNum);
            if (indication != null)
            {
                var textToSpeechService = CompletionProvider.GetTextToSpeech(_services, "openai", "tts-1");
                var fileService = _services.GetRequiredService<IFileStorageService>();
                var data = await textToSpeechService.GenerateSpeechFromTextAsync(indication);
                var fileName = $"indication_{seqNum}.mp3";
                await fileService.SaveSpeechFileAsync(conversationId, fileName, data);
                response = twilio.ReturnInstructions($"twilio/voice/speeches/{conversationId}/{fileName}", $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 2);
            }
            else
            {
                response = twilio.ReturnInstructions(null, $"twilio/voice/{conversationId}/reply/{seqNum}?states={states}", true, 1);
            }
        }
        else
        {
            var textToSpeechService = CompletionProvider.GetTextToSpeech(_services, "openai", "tts-1");
            var fileService = _services.GetRequiredService<IFileStorageService>();
            var data = await textToSpeechService.GenerateSpeechFromTextAsync(reply.Content);
            var fileName = $"reply_{seqNum}.mp3";
            await fileService.SaveSpeechFileAsync(conversationId, fileName, data);
            if (reply.ConversationEnd)
            {
                response = twilio.HangUp($"twilio/voice/speeches/{conversationId}/{fileName}");
            }
            else
            {
                response = twilio.ReturnInstructions($"twilio/voice/speeches/{conversationId}/{fileName}", $"twilio/voice/{conversationId}/receive/{nextSeqNum}?states={states}", true);
            }

        }
        return TwiML(response);
    }

    [HttpGet("speeches/{conversationId}/{fileName}")]
    public async Task<FileContentResult> RetrieveSpeechFile([FromRoute] string conversationId, [FromRoute] string fileName)
    {
        var fileService = _services.GetRequiredService<IFileStorageService>();
        var data = await fileService.RetrieveSpeechFileAsync(conversationId, fileName);
        var result = new FileContentResult(data.ToArray(), "audio/mpeg");
        result.FileDownloadName = fileName;
        return result;
    }
}
