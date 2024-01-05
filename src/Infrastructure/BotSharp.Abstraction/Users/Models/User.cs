using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.Abstraction.Users.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string Role { get; set; } = UserRole.Client;
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
