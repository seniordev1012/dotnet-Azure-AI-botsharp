namespace BotSharp.Plugin.EmailHandler.Settings;

public class EmailSenderSettings
{
    public string EmailAddress { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SMTPServer { get; set; } = string.Empty;
    public int SMTPPort { get; set; }
}
