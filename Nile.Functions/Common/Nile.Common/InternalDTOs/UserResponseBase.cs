namespace Nile.Common.InternalDTOs
{
    public abstract class UserResponseBase : ResponseBase
    {
        public Guid[] Id { get; set; } = Array.Empty<Guid>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? NextPageToken { get; set; }
        public bool HasMore { get; set; }

    }
}