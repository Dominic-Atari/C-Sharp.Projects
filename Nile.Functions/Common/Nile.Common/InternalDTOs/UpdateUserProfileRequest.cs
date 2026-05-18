namespace Nile.Common.InternalDTOs;

public class UpdateUserProfileRequest : UserRequestBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
