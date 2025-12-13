using System.Text.Json.Serialization;

namespace Nile.Managers.Contract.Client.DataContract.V1.User;
[JsonDerivedType(typeof(UpdateUserProfileRequest), typeDiscriminator: nameof(UpdateUserProfileRequest))]
[JsonDerivedType(typeof(StoreUserProfileImageRequest), typeDiscriminator: nameof(StoreUserProfileImageRequest))]
[JsonDerivedType(typeof(StoreNotificationPreferencesRequest), typeDiscriminator: nameof(StoreNotificationPreferencesRequest))]
public class StoreUserRequestBase : RequestBase
{
}