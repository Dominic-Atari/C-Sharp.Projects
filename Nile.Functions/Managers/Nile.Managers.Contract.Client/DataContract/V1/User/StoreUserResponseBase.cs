namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class StoreUserResponseBase : ResponseBase
{
	public Guid[] Id { get; set; } = Array.Empty<Guid>();
	public DateTime CreatedAt { get; set; }
	public string? NextPageToken { get; set; }
	public bool HasMore { get; set; }
}