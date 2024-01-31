namespace BotSharp.Plugin.MongoStorage.Collections;

public class UserDocument : MongoBase
{
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Salt { get; set; }
    public string Password { get; set; }
    public string Source { get; set; } = "internal";
    public string? ExternalId { get; set; }
    public string Role { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}