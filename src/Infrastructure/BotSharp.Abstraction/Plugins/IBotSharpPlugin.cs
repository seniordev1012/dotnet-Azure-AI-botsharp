using BotSharp.Abstraction.Plugins.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Plugins;

public interface IBotSharpPlugin
{
    /// <summary>
    /// Plugin id (guid)
    /// </summary>
    string Id { get; }
    string Name => "";
    string Description => "";
    string IconUrl => "https://avatars.githubusercontent.com/u/44989469?s=200&v=4";

    /// <summary>
    /// Has build-in agent profile with this plugin
    /// </summary>
    bool WithAgent => false;

    void RegisterDI(IServiceCollection services, IConfiguration config);

    PluginMenuDef[] GetMenus() => new PluginMenuDef[0];
}
