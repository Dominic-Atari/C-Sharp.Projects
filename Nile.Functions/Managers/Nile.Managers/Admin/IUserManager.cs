using Nile.Managers.Contract.Client.DataContract.V1.User;

namespace Nile.Managers.Admin;

public interface IUserManager
{
    Task<UserContextResponse> Login(LoginRequest request);

    Task<StoreUserResponseBase> Store(CreateUserProfileRequest request);
}