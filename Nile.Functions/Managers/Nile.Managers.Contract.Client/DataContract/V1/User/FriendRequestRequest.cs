using System.ComponentModel.DataAnnotations;

namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class FriendRequestRequest : RequestBase
{
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public required string Username { get; init; }
}