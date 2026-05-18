using Nile.Managers.Contract.Client.DataContract.V1.User;

namespace Nile.Managers.Admin;

public interface IUserManager
{
    Task<UserContextResponse> Login(LoginRequest request);

    Task<UsernameSuggestionsResponse> GenerateUsernameSuggestions(UsernameSuggestionsRequest request);

    Task<StoreUserResponseBase> Store(CreateUserProfileRequest request);

    Task<StoreUserResponseBase> Store(UpdateUserProfileRequest request);

    Task<StoreUserResponseBase> Store(StoreUserProfileImageRequest request);

    Task<StoreUserResponseBase> Store(DeleteUserProfileImageRequest request);

    Task<StoreUserResponseBase> Store(StoreNotificationPreferencesRequest request);
}