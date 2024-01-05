using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.ViewModels.Users;

public class UserCreationModel
{
    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;   
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Client;

    public User ToUser()
    {
        return new User 
        { 
            UserName = UserName,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Password = Password,
            Role = Role
        };
    }
}
