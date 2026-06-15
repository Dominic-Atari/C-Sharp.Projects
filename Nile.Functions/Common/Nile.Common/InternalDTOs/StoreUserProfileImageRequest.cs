namespace Nile.Common.InternalDTOs;

public class StoreUserProfileImageRequest : UserRequestBase
{
    public string ImageFilename { get; set; } = string.Empty;
}
