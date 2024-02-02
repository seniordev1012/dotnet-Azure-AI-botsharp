using BotSharp.Abstraction.Routing;
using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class ClickElementFn : IFunctionCallback
{
    public string Name => "click_element";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public ClickElementFn(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        /*if (args.ElementType == "button")
        {
            var fn = _services.GetRequiredService<IRoutingService>();
            return await fn.InvokeFunction("click_button", message);
        }*/

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        await _driver.ClickElement(agent, args, message.MessageId);

        message.Content = $"Element with text \"{args.InputText}\" is clicked successfully.";
        return true;
    }
}
