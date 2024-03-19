using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Enums;
using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json;

namespace BotSharp.Core.Messaging;

public class MessageParser
{
    public MessageParser()
    {
    }

    public IRichMessage? ParseRichMessage(string richType, string jsonText, JsonSerializerOptions options)
    {
        IRichMessage? res = null;

        if (richType == RichTypeEnum.ButtonTemplate)
        {
            res = JsonSerializer.Deserialize<ButtonTemplateMessage>(jsonText, options);
        }
        else if (richType == RichTypeEnum.MultiSelectTemplate)
        {
            res = JsonSerializer.Deserialize<MultiSelectTemplateMessage>(jsonText, options);
        }
        else if (richType == RichTypeEnum.QuickReply)
        {
            res = JsonSerializer.Deserialize<QuickReplyMessage>(jsonText, options);
        }
        else if (richType == RichTypeEnum.CouponTemplate)
        {
            res = JsonSerializer.Deserialize<CouponTemplateMessage>(jsonText, options);
        }
        else if (richType == RichTypeEnum.Text)
        {
            res = JsonSerializer.Deserialize<TextMessage>(jsonText, options);
        }

        return res;
    }

    public ITemplateMessage? ParseTemplateMessage(string templateType, string jsonText, JsonSerializerOptions options)
    {
        ITemplateMessage? res = null;

        if (templateType == TemplateTypeEnum.Button)
        {
            res = JsonSerializer.Deserialize<ButtonTemplateMessage>(jsonText, options);
        }
        else if (templateType == TemplateTypeEnum.MultiSelect)
        {
            res = JsonSerializer.Deserialize<MultiSelectTemplateMessage>(jsonText, options);
        }
        else if (templateType == TemplateTypeEnum.Coupon)
        {
            res = JsonSerializer.Deserialize<CouponTemplateMessage>(jsonText, options);
        }
        else if (templateType == TemplateTypeEnum.Product)
        {
            res = JsonSerializer.Deserialize<ProductTemplateMessage>(jsonText, options);
        }

        return res;
    }
}
