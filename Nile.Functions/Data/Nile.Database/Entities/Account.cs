namespace Nile.Database.Entities;

public class Account
{
    // Primary Key
    public Guid AccountId { get; init; }

    // Foreign Key -> Users(Id)
    public Guid UserId { get; init; }

    // Contact/identifiers
    public string EmailAddress { get; init; } = null!;
    public string? ExternalAuthId { get; init; }
    public Guid? ExternalCustomerId { get; init; }

    // Status/flags
    public int Status { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}