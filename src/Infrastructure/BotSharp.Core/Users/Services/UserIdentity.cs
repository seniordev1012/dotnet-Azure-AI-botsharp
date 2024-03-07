using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace BotSharp.Core.Users.Services;

public class UserIdentity : IUserIdentity
{
    private readonly IHttpContextAccessor _contextAccessor;
    private IEnumerable<Claim> _claims => _contextAccessor.HttpContext?.User.Claims!;

    public UserIdentity(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }


    public string Id
        => _claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value!;

    [JsonPropertyName("user_name")]
    public string UserName
        => _claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value!;

    public string Email
        => _claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value!;

    [JsonPropertyName("first_name")]
    public string FirstName
    {
        get
        {
            var givenName = _claims?.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
            if (givenName == null)
            {
                return UserName;
            }
            return givenName.Value;
        }
    }

    [JsonPropertyName("last_name")]
    public string LastName 
        => _claims?.FirstOrDefault(x => x.Type == ClaimTypes.Surname)?.Value!;

    [JsonPropertyName("full_name")]
    public string FullName
    {
        get
        {
            var fullName = _claims?.FirstOrDefault(x => x.Type == "full_name")?.Value;
            if (!string.IsNullOrEmpty(fullName))
            {
                return fullName;
            }
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
