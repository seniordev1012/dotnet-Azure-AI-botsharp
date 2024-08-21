using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace BotSharp.Core.Infrastructures;

public class DistributedLocker
{
    private readonly BotSharpDatabaseSettings _settings;

    public DistributedLocker(BotSharpDatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<T> Lock<T>(string resource, Func<Task<T>> action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var connection = await ConnectionMultiplexer.ConnectAsync(_settings.Redis);
        var @lock = new RedisDistributedLock(resource, connection.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle == null) 
            {
                Serilog.Log.Logger.Error($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
            }
            
            return await action();
        }
    }

    public async Task Lock(string resource, Action action, int timeoutInSeconds = 30)
    {
        var timeout = TimeSpan.FromSeconds(timeoutInSeconds);

        var connection = await ConnectionMultiplexer.ConnectAsync(_settings.Redis);
        var @lock = new RedisDistributedLock(resource, connection.GetDatabase());
        await using (var handle = await @lock.TryAcquireAsync(timeout))
        {
            if (handle != null)
            {
                action();
            }
            else
            {
                Serilog.Log.Logger.Error($"Acquire lock for {resource} failed due to after {timeout}s timeout.");
            }
        }
    }
}
