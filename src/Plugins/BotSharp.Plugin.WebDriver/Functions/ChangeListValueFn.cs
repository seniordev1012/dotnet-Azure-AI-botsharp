namespace BotSharp.Plugin.WebDriver.Functions;

public class ChangeListValueFn : IFunctionCallback
{
    public string Name => "change_list_value";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ChangeListValueFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        var result = await _browser.ChangeListValue(new BrowserActionParams(agent, args, message.MessageId));

        message.Content = result ? "Success" : "Failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.NewScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
