using Microsoft.AspNetCore.Identity;

namespace TasksManagement.API.Models.Entities;

public class User : IdentityUser<Guid>
{
    public string Names { get; set; }
    public string LastNames { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public User(string names, string lastNames)
    {
        Names = names;
        LastNames = lastNames;
    }
    public User()
    {

    }
}
