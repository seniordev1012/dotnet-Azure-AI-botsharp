namespace BotSharp.Abstraction.Repositories;

public class MyDatabaseSettings : DatabaseSettings
{
    public string[] Assemblies { get; set; }
    public string FileRepository { get; set; }
    public string MongoDb { get; set; }
    public DbConnectionSetting BotSharp { get; set; }
}

public class DatabaseSettings
{
    public string Default { get; set; }
    public DbConnectionSetting DefaultConnection { get; set; }
    public bool EnableSqlLog { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableRetryOnFailure { get; set; }
    public bool UseCamelCase { get; set; }
}

public class DbConnectionSetting
{
    public string Master { get; set; }

    public string[] Slavers { get; set; }

    public DbConnectionSetting()
    {
        Slavers = new string[0];
    }
}
