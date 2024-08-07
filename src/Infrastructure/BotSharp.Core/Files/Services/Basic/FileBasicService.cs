using System.IO;

namespace BotSharp.Core.Files.Services;

public partial class FileBasicService : IFileBasicService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly ILogger<FileBasicService> _logger;
    private readonly string _baseDir;
    private readonly IEnumerable<string> _imageTypes = new List<string>
    {
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Jpeg
    };

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";
    private const string USER_FILE_FOLDER = "user";
    private const string SCREENSHOT_FILE_FOLDER = "screenshot";
    private const string BOT_FILE_FOLDER = "bot";
    private const string USERS_FOLDER = "users";
    private const string USER_AVATAR_FOLDER = "avatar";
    private const string SESSION_FOLDER = "sessions";

    public FileBasicService(
        BotSharpDatabaseSettings dbSettings,
        IUserIdentity user,
        ILogger<FileBasicService> logger,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _user = user;
        _logger = logger;
        _services = services;
        _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository);
    }
}
