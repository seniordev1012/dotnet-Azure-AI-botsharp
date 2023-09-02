using BotSharp.Plugin.MongoStorage.Repository;

namespace BotSharp.Plugin.MongoStorage;

public class MongoStoragePlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped((IServiceProvider x) =>
        {
            var dbSettings = x.GetRequiredService<BotSharpDatabaseSettings>();
            return new MongoDbContext(dbSettings.MongoDb);
        });

        services.AddScoped<IBotSharpRepository, MongoRepository>();
    }
}
