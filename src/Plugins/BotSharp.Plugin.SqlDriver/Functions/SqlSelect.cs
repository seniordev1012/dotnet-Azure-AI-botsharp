using BotSharp.Plugin.SqlDriver.Models;
using MySqlConnector;
using static Dapper.SqlMapper;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class SqlSelect : IFunctionCallback
{
    public string Name => "sql_select";
    private readonly IServiceProvider _services;

    public SqlSelect(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        var sqlDriver = _services.GetRequiredService<SqlDriverService>();

        // check if need to instantely
        var execNow = !args.Parameters.Any(x => x.Value.StartsWith("@"));
        if (execNow)
        {
            var settings = _services.GetRequiredService<SqlDriverSetting>();
            using var connection = new MySqlConnection(settings.MySqlConnectionString);
            var dictionary = new Dictionary<string, object>();
            foreach(var p in args.Parameters)
            {
                dictionary["@" + p.Name] = p.Value;
            }
            var result = connection.QueryFirstOrDefault(args.Statement, dictionary);

            if (result == null)
            {
                message.Content = "Record not found";
            }
            else
            {
                message.Content = JsonSerializer.Serialize(result);
                args.Return.Value = message.Content;
            }
            
            sqlDriver.Enqueue(args);
        }
        else
        {
            sqlDriver.Enqueue(args);
            message.Content = $"The {args.Return.Name} is saved to @{args.Return.Alias}";
        }
        
        return true;
    }
}
