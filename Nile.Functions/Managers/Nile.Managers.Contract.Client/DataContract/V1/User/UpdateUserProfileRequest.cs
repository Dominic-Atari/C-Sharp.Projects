using System.ComponentModel.DataAnnotations;

namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UpdateUserProfileRequest : StoreUserRequestBase
{
    [Required]
    [StringLength(maximumLength: 24, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required]
    [StringLength(maximumLength: 24, MinimumLength = 2)]
    public required string LastName { get; init; }

    [Required]
    [StringLength(30)]
    [RegularExpression(Patterns.Username)]
    public required string Username { get; set; }
}