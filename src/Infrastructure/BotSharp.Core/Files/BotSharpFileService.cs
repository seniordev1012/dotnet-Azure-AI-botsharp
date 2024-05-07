using System.IO;
using System.Threading;

namespace BotSharp.Core.Files;

public class BotSharpFileService : IBotSharpFileService
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    private readonly string _baseDir;

    private const string CONVERSATION_FOLDER = "conversations";
    private const string FILE_FOLDER = "files";

    public BotSharpFileService(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
        _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbSettings.FileRepository);
    }

    public string GetDirectory(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, CONVERSATION_FOLDER, conversationId, "attachments");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public IEnumerable<OutputFileModel> GetConversationFiles(string conversationId, string messageId)
    {
        var outputFiles = new List<OutputFileModel>();
        var dir = GetConversationFileDirectory(conversationId, messageId);
        if (string.IsNullOrEmpty(dir))
        {
            return outputFiles;
        }

        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);
            var fileType = extension.Substring(1);
            var model = new OutputFileModel()
            {
                FileUrl = $"/conversation/{conversationId}/message/{messageId}/file/{fileName}",
                FileName = fileName,
                FileType = fileType
            };
            outputFiles.Add(model);
        }
        return outputFiles;
    }

    public string? GetMessageFile(string conversationId, string messageId, string fileName)
    {
        var dir = GetConversationFileDirectory(conversationId, messageId);
        if (string.IsNullOrEmpty(dir))
        {
            return null;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public void SaveMessageFiles(string conversationId, string messageId, List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return;

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);
        if (string.IsNullOrEmpty(dir)) return;

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (string.IsNullOrEmpty(file.FileData))
            {
                continue;
            }

            var bytes = GetFileBytes(file.FileData);
            var fileType = Path.GetExtension(file.FileName);
            var fileName = $"{i + 1}{fileType}";
            Thread.Sleep(100);
            File.WriteAllBytes(Path.Combine(dir, fileName), bytes);
        }
    }

    public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null)
    {
        if (string.IsNullOrEmpty(conversationId) ||  messageIds == null) return false;

        if (!string.IsNullOrEmpty(targetMessageId) && !string.IsNullOrEmpty(newMessageId))
        {
            var prevDir = GetConversationFileDirectory(conversationId, targetMessageId);
            var newDir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, newMessageId);

            if (Directory.Exists(prevDir))
            {
                if (Directory.Exists(newDir))
                {
                    Directory.Delete(newDir, true);
                }

                Directory.Move(prevDir, newDir);
            }
        }

        foreach ( var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (string.IsNullOrEmpty(dir)) continue;

            Thread.Sleep(100);
            Directory.Delete(dir, true);
        }

        return true;
    }

    public bool DeleteConversationFiles(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        foreach (var conversationId in conversationIds)
        {
            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) continue;

            Directory.Delete(convDir, true);
        }
        return true;
    }

    #region Private methods
    private string GetConversationFileDirectory(string? conversationId, string? messageId, bool createNewDir = false)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return string.Empty;
        }

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId);
        if (!Directory.Exists(dir))
        {
            if (createNewDir)
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                return string.Empty;
            }
        }
        return dir;
    }

    private string? FindConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId);
        if (!Directory.Exists(dir)) return null;

        return dir;
    }

    private byte[] GetFileBytes(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return new byte[0];
        }

        var startIdx = data.IndexOf(',');
        var base64Str = data.Substring(startIdx + 1);
        return Convert.FromBase64String(base64Str);
    }

    private string GetFileType(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        var startIdx = data.IndexOf(':');
        var endIdx = data.IndexOf(';');
        var fileType = data.Substring(startIdx + 1, endIdx - startIdx - 1);
        return fileType;
    }

    private string ParseFileFormat(string type)
    {
        var parsed = string.Empty;
        switch (type)
        {
            case "image/png":
                parsed = ".png";
                break;
            case "image/jpeg":
            case "image/jpg":
                parsed = ".jpeg";
                break;
            case "application/pdf":
                parsed = ".pdf";
                break;
            case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                parsed = ".xlsx";
                break;
            case "text/plain":
                parsed = ".txt";
                break;
        }
        return parsed;
    }
    #endregion
}
