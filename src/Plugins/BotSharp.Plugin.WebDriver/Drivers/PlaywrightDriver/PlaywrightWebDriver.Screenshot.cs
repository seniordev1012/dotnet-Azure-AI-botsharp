namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ScreenshotAsync(string conversationId, string path)
    {
        var result = new BrowserActionResult();

        await _instance.Wait(conversationId);
        var page = _instance.GetPage(conversationId);

        await Task.Delay(500);
        var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        result.IsSuccess = true;
        result.Body = "data:image/png;base64," + Convert.ToBase64String(bytes);
        return result;
    }
}
