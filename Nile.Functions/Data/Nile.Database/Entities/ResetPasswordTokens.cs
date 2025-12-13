namespace Nile.Database.Entities;

public class ResetPasswordTokens
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}