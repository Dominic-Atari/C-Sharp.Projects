using System.ComponentModel.DataAnnotations;
using Nile.Common.InternalDTOs;

namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UpdateFriendRequestRequest : StoreUserRequestBase
{
    [Required]
    public required Guid TargetUserId { get; init; }

    [Required]
    public required FriendRequestActionType Action { get; init; }
}