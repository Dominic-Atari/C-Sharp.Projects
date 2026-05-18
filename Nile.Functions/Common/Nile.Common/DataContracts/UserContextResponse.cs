
namespace Nile.Common.DataContracts;

public class UserContextResponse : UserResponseBase
{
    public LoginUserProfile? Profile { get; init; }
}