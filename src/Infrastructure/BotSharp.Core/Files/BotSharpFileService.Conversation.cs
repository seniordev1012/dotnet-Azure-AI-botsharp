using BotSharp.Abstraction.Files.Converters;
using BotSharp.Core.Files.Converters;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace BotSharp.Core.Files;

public partial class BotSharpFileService
{
    public async Task<IEnumerable<MessageFileModel>> GetChatImages(string conversationId, string source, IEnumerable<string> fileTypes,
        List<RoleDialogModel> conversations, int? offset = null)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || conversations.IsNullOrEmpty())
        {
            return new List<MessageFileModel>();
        }

        if (offset <= 0)
        {
            offset = MIN_OFFSET;
        }
        else if (offset > MAX_OFFSET)
        {
            offset = MAX_OFFSET;
        }

        var messageIds = new List<string>();
        if (offset.HasValue)
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().TakeLast(offset.Value).ToList();
        }
        else
        {
            messageIds = conversations.Select(x => x.MessageId).Distinct().ToList();
        }

        files = await GetMessageFiles(conversationId, messageIds, source, fileTypes);
        return files;
    }

    private async Task<List<MessageFileModel>> GetMessageFiles(string conversationId, IEnumerable<string> messageIds, string source, IEnumerable<string> fileTypes)
    {
        var files = new List<MessageFileModel>();
        if (string.IsNullOrEmpty(conversationId) || messageIds.IsNullOrEmpty() || fileTypes.IsNullOrEmpty()) return files;

        var isNeedScreenShot = fileTypes.Any(x => _allowScreenShotTypes.Contains(x));
        var onlyScreenShot = fileTypes.All(x => _allowScreenShotTypes.Contains(x));

        try
        {
            var preFixPath = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);

            foreach (var messageId in messageIds)
            {
                var dir = Path.Combine(preFixPath, messageId, source);
                if (!ExistDirectory(dir)) continue;

                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    var file = Directory.GetFiles(subDir).FirstOrDefault();
                    if (file == null) continue;

                    var index = subDir.Split(Path.DirectorySeparatorChar).Last();
                    var contentType = GetFileContentType(file);

                    if ((!isNeedScreenShot || (isNeedScreenShot && !onlyScreenShot)) && _allowedImageTypes.Contains(contentType))
                    {
                        var model = new MessageFileModel()
                        {
                            MessageId = messageId,
                            FileStorageUrl = file,
                            ContentType = contentType
                        };
                        files.Add(model);
                    }
                    else if ((isNeedScreenShot && !onlyScreenShot || onlyScreenShot) && !_allowedImageTypes.Contains(contentType))
                    {
                        var screenShotDir = Path.Combine(subDir, SCREENSHOT_FILE_FOLDER);
                        if (ExistDirectory(screenShotDir) && Directory.GetFiles(screenShotDir).Any())
                        {
                            foreach (var screenShot in Directory.GetFiles(screenShotDir))
                            {
                                contentType = GetFileContentType(screenShot);
                                if (!_allowedImageTypes.Contains(contentType)) continue;

                                var model = new MessageFileModel()
                                {
                                    MessageId = messageId,
                                    FileStorageUrl = screenShot,
                                    ContentType = contentType
                                };
                                files.Add(model);
                            }
                        }
                        else
                        {
                            var screenShotPath = Path.Combine(subDir, SCREENSHOT_FILE_FOLDER);
                            var images = await ConvertPdfToImages(file, screenShotPath);

                            foreach (var image in images)
                            {
                                contentType = GetFileContentType(image);
                                var model = new MessageFileModel()
                                {
                                    MessageId = messageId,
                                    FileStorageUrl = image,
                                    ContentType = contentType
                                };
                                files.Add(model);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when reading conversation ({conversationId}) files: {ex.Message}");
        }

        return files;
    }

    public IEnumerable<MessageFileModel> GetMessageFiles(string conversationId, IEnumerable<string> messageIds,
        string source, bool imageOnly = false)
    {
        var files = new List<MessageFileModel>();
        if (messageIds.IsNullOrEmpty()) return files;

        foreach (var messageId in messageIds)
        {
            var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId, source);
            if (!ExistDirectory(dir))
            {
                continue;
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                var index = subDir.Split(Path.DirectorySeparatorChar).Last();

                foreach (var file in Directory.GetFiles(subDir))
                {
                    var contentType = GetFileContentType(file);
                    if (imageOnly && !_allowedImageTypes.Contains(contentType))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var extension = Path.GetExtension(file);
                    var fileType = extension.Substring(1);

                    var model = new MessageFileModel()
                    {
                        MessageId = messageId,
                        FileUrl = $"/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}",
                        FileStorageUrl = file,
                        FileName = fileName,
                        FileType = fileType,
                        ContentType = contentType
                    };
                    files.Add(model);
                }
            }
        }

        return files;
    }

    public string GetMessageFile(string conversationId, string messageId, string source, string index, string fileName)
    {
        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, messageId, source, index);
        if (!ExistDirectory(dir))
        {
            return string.Empty;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).IsEqualTo(fileName));
        return found;
    }

    public bool HasConversationUserFiles(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER);
        if (!ExistDirectory(dir)) return false;

        return Directory.GetDirectories(dir).Any();
    }

    public bool SaveMessageFiles(string conversationId, string messageId, string source, List<BotSharpFile> files)
    {
        if (files.IsNullOrEmpty()) return false;

        var dir = GetConversationFileDirectory(conversationId, messageId, createNewDir: true);
        if (!ExistDirectory(dir)) return false;

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (string.IsNullOrEmpty(file.FileData))
                {
                    continue;
                }

                var (_, bytes) = GetFileInfoFromData(file.FileData);
                var subDir = Path.Combine(dir, source, $"{i + 1}");
                if (!ExistDirectory(subDir))
                {
                    Directory.CreateDirectory(subDir);
                }

                using (var fs = new FileStream(Path.Combine(subDir, file.FileName), FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                    fs.Close();
                    Thread.Sleep(100);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when saving conversation files: {ex.Message}");
            return false;
        }
    }


    public bool DeleteMessageFiles(string conversationId, IEnumerable<string> messageIds, string targetMessageId, string? newMessageId = null)
    {
        if (string.IsNullOrEmpty(conversationId) || messageIds == null) return false;

        if (!string.IsNullOrEmpty(targetMessageId) && !string.IsNullOrEmpty(newMessageId))
        {
            var prevDir = GetConversationFileDirectory(conversationId, targetMessageId);
            var newDir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId, FILE_FOLDER, newMessageId);

            if (ExistDirectory(prevDir))
            {
                if (ExistDirectory(newDir))
                {
                    Directory.Delete(newDir, true);
                }

                Directory.Move(prevDir, newDir);
                Thread.Sleep(100);

                var botDir = Path.Combine(newDir, BOT_FILE_FOLDER);
                if (ExistDirectory(botDir))
                {
                    Directory.Delete(botDir, true);
                }
            }
        }

        foreach (var messageId in messageIds)
        {
            var dir = GetConversationFileDirectory(conversationId, messageId);
            if (!ExistDirectory(dir)) continue;

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
            var convDir = GetConversationDirectory(conversationId);
            if (!ExistDirectory(convDir)) continue;

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
        if (!Directory.Exists(dir) && createNewDir)
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    private string? GetConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_baseDir, CONVERSATION_FOLDER, conversationId);
        return dir;
    }

    private async Task<IEnumerable<string>> ConvertPdfToImages(string pdfLoc, string imageLoc)
    {
        var converters = _services.GetServices<IPdf2ImageConverter>();
        if (converters.IsNullOrEmpty()) return Enumerable.Empty<string>();

        var converter = GetPdf2ImageConverter();
        if (converter == null)
        {
            return Enumerable.Empty<string>();
        }
        return await converter.ConvertPdfToImages(pdfLoc, imageLoc);
    }

    private IPdf2ImageConverter? GetPdf2ImageConverter()
    {
        var converters = _services.GetServices<IPdf2ImageConverter>();
        var converter = converters.FirstOrDefault(x => x.GetType().Name != typeof(PdfiumConverter).Name);
        if (converter == null)
        {
            converter = converters.FirstOrDefault(x => x.GetType().Name == typeof(PdfiumConverter).Name);
        }
        return converter;
    }
    #endregion
}
