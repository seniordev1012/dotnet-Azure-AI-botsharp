namespace BotSharp.Plugin.MongoStorage.Collections;

public class UserDocument : MongoBase
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Salt { get; set; }
    public string Password { get; set; }
    public string? ExternalId { get; set; }
    public string Role { get; set; }

    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}