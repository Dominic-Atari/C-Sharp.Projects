using System.ComponentModel.DataAnnotations;

namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class StoreUserProfileImageRequest : StoreUserRequestBase
{
    [Required]
    public required string ImageFilename { get; set; }
}