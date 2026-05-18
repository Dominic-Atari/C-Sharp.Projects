namespace Nile.Database.Entities;

public class Passwords
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = null!;

    // Navigation
    public User User { get; set; } = null!;
}