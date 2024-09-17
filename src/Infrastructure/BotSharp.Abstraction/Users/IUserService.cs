using BotSharp.Abstraction.Users.Models;
using BotSharp.OpenAPI.ViewModels.Users;

namespace BotSharp.Abstraction.Users;

public interface IUserService
{
    Task<User> GetUser(string id);
    Task<User> CreateUser(User user);
    Task<Token> ActiveUser(UserActivationModel model);
    Task<Token?> GetToken(string authorization);
    Task<User> GetMyProfile();
    Task<bool> VerifyUserNameExisting(string userName);
    Task<bool> VerifyEmailExisting(string email);
    Task<bool> SendVerificationCodeResetPassword(User user);
    Task<bool> ResetUserPassword(User user);
    Task<bool> ModifyUserEmail(string email);
    Task<bool> ModifyUserPhone(string phone);
    Task<DateTime> GetUserTokenExpires();
}