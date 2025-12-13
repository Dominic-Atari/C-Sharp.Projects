using Nile.Managers.Contract.Client.DataContract.V1.User;

namespace Nile.Managers.Admin;

public interface IUserManager
{
    Task<UsernameSuggestionsResponse> GenerateUsernameSuggestions(UsernameSuggestionsRequest request);

    Task<UserContextResponse> Login(LoginRequest request);

    // General store operation for user-related updates
    Task<StoreUserResponseBase> Store(StoreUserRequestBase request);

    // Specific overloads used by clients
    Task<StoreUserResponseBase> Store(CreateUserProfileRequest request);

    Task<StoreUserResponseBase> Store(StoreUserProfileImageRequest request);

    Task<StoreUserResponseBase> Store(DeleteUserProfileImageRequest request);
}