namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ClickElement(BrowserActionParams actionParams)
    {
        var result = new BrowserActionResult();
        await _instance.Wait(actionParams.ConversationId);

        var page = _instance.GetPage(actionParams.ConversationId);
        ILocator locator = default;
        int count = 0;

        // Retrieve the page raw html and infer the element path
        if (!string.IsNullOrEmpty(actionParams.Context.ElementText))
        {
            var regexExpression = actionParams.Context.MatchRule.ToLower() switch
            {
                "startwith" => $"^{actionParams.Context.ElementText}",
                "endwith" => $"{actionParams.Context.ElementText}$",
                "contains" => $"{actionParams.Context.ElementText}",
                _ => $"^{actionParams.Context.ElementText}$"
            };
            var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
            locator = page.GetByText(regex);
            count = await locator.CountAsync();

            // try placeholder
            if (count == 0)
            {
                locator = page.GetByPlaceholder(regex);
                count = await locator.CountAsync();
            }
        }

        // try attribute
        if (count == 0 && !string.IsNullOrEmpty(actionParams.Context.AttributeName))
        {
            locator = page.Locator($"[{actionParams.Context.AttributeName}='{actionParams.Context.AttributeValue}']");
            count = await locator.CountAsync();
        }

        if (count == 0)
        {
            result.ErrorMessage = $"Can't locate element by keyword {actionParams.Context.ElementText}";
            _logger.LogError(result.ErrorMessage);
        }
        else if (count == 1)
        {
            // var tagName = await locator.EvaluateAsync<string>("el => el.tagName");
            await locator.ClickAsync();

            // Triggered ajax
            await _instance.Wait(actionParams.ConversationId);

            result.IsSuccess = true;
        }
        else if (count > 1)
        {
            result.ErrorMessage = $"Multiple elements are found by keyword {actionParams.Context.ElementText}";
            _logger.LogWarning(result.ErrorMessage);
            var all = await locator.AllAsync();
            foreach (var element in all)
            {
                var content = await element.InnerHTMLAsync();
                _logger.LogWarning(content);
            }
        }

        return result;
    }
}
