using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.MongoStorage.Collections;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public User? GetUserById(string id)
    {
        var user = _dc.Users.AsQueryable()
            .FirstOrDefault(x => x.Id == id || (x.ExternalId != null && x.ExternalId == id));
        return user != null ? user.ToUser() : null;
    }

    public User? GetUserByUserName(string userName)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.UserName == userName.ToLower());
        return user != null ? user.ToUser() : null;
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userCollection = new UserDocument
        {
            Id = user.Id ?? Guid.NewGuid().ToString(),
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            Source = user.Source,
            ExternalId = user.ExternalId,
            Role = user.Role,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }
}
