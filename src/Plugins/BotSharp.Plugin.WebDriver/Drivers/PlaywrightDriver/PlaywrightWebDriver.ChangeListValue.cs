using BotSharp.Plugin.WebDriver.Services;
using System.Threading;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ChangeListValue(Agent agent, BrowsingContextIn context, string messageId)
    {
        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        var inputs = await body.QuerySelectorAllAsync("select");
        foreach (var input in inputs)
        {
            var html = "<select";
            var id = await input.GetAttributeAsync("id");
            if (!string.IsNullOrEmpty(id))
            {
                html += $" id='{id}'";
            }
            var name = await input.GetAttributeAsync("name");
            if (!string.IsNullOrEmpty(name))
            {
                html += $" name='{id}'";
            }
            html += ">";

            var options = await input.QuerySelectorAllAsync("option");
            if (options != null)
            {
                foreach (var option in options)
                {
                    html += "<option";
                    var value = await option.GetAttributeAsync("value");
                    if (!string.IsNullOrEmpty(value))
                    {
                        html += $" value='{value}'";
                    }
                    html += ">";
                    var text = await option.TextContentAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        html += text;
                    }
                    else
                    {
                        html += "'<NULL>'";
                    }
                    html += "</option>";
                }
            }

            html += "</select>";
            str.Add(html);
        }

        var driverService = _services.GetRequiredService<WebDriverService>();
        var htmlElementContextOut = await driverService.LocateElement(agent, string.Join("", str), context.ElementName, messageId);

        if (htmlElementContextOut.Index < 0)
        {
            throw new Exception($"Can't locate the web element {context.ElementName}.");
        }

        ILocator element = default;
        if (!string.IsNullOrEmpty(htmlElementContextOut.ElementId))
        {
            // await _instance.Page.WaitForSelectorAsync($"#{htmlElementContextOut.ElementId}", new PageWaitForSelectorOptions { Timeout = 3 });
            element = _instance.Page.Locator($"#{htmlElementContextOut.ElementId}");
        }
        else
        {
            element = _instance.Page.Locator(htmlElementContextOut.TagName).Nth(htmlElementContextOut.Index);
        }
        
        try
        {
            var isVisible = await element.IsVisibleAsync();

            if (!isVisible)
            {
                // Select the element you want to make visible (replace with your own selector)
                var control = await _instance.Page.QuerySelectorAsync($"#{htmlElementContextOut.ElementId}");

                // Show the element by modifying its CSS styles
                await _instance.Page.EvaluateAsync(@"(element) => {
                    element.style.display = 'block';
                    element.style.visibility = 'visible';
                }", control);
            }

            await element.FocusAsync();
            await element.SelectOptionAsync(new SelectOptionValue
            {
                Label = context.UpdateValue
            });
            
            // Click on the blank area to activate posting
            await body.ClickAsync();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
