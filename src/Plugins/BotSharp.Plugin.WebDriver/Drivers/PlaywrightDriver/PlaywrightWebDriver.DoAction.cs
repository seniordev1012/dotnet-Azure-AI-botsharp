namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ContextId);
        if (string.IsNullOrEmpty(result.Selector))
        {
            Serilog.Log.Error($"Selector is not set.");
            return;
        }

        ILocator locator = page.Locator(result.Selector);
        var count = await locator.CountAsync();
        if (count == 0)
        {
            Serilog.Log.Error($"Element not found: {result.Selector}");
            return;
        }

        if (action.Action == BroswerActionEnum.Click)
        {
            if (action.Position == null)
            {
                await locator.ClickAsync();
            }
            else
            {
                await locator.ClickAsync(new LocatorClickOptions
                {
                    Position = new Position
                    {
                        X = action.Position.X,
                        Y = action.Position.Y
                    }
                });
            }
        }
        else if (action.Action == BroswerActionEnum.InputText)
        {
            await locator.FillAsync(action.Content);

            if (action.PressKey != null)
            {
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Typing)
        {
            await locator.PressSequentiallyAsync(action.Content);
            if (action.PressKey != null)
            {
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Hover)
        {
            await locator.HoverAsync();
        }

        if (action.WaitTime > 0)
        {
            await Task.Delay(1000 * action.WaitTime);
        }
    }
}
