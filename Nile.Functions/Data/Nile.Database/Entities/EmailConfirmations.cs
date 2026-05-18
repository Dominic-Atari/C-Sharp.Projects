namespace Nile.Database.Entities;

public class EmailConfirmations
{
    public Guid UserId { get; set; }
    public string ConfirmationToken { get; set; } = null!;
    public bool IsConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}