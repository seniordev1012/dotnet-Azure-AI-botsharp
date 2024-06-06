namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> InputUserPassword(BrowserActionParams actionParams)
    {
        var result = new BrowserActionResult();
        await _instance.Wait(actionParams.ContextId);

        // Retrieve the page raw html and infer the element path
        var body = await _instance.GetPage(actionParams.ContextId)
            .QuerySelectorAsync("body");

        var inputs = await body.QuerySelectorAllAsync("input");
        var password = inputs.FirstOrDefault(x => x.GetAttributeAsync("type").Result == "password");

        if (password == null)
        {
            result.Message = $"Can't locate the password element by '{actionParams.Context.ElementName}'";
            _logger.LogError(result.Message);
            return result;
        }

        var config = _services.GetRequiredService<IConfiguration>();
        try
        {
            await password.FillAsync(actionParams.Context.Password);
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }

        return result;
    }
}
