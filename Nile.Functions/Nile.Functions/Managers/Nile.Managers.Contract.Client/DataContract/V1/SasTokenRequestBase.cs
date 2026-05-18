using System.Text.Json.Serialization;
using Nile.Managers.Contract.Client.DataContract.V1.SasTokens;

namespace Nile.Managers.Contract.Client.DataContract.V1;

[JsonDerivedType(typeof(OcrSasTokenRequest), typeDiscriminator: nameof(OcrSasTokenRequest))]
[JsonDerivedType(typeof(PostPhotoSasTokenRequest), typeDiscriminator: nameof(PostPhotoSasTokenRequest))]
[JsonDerivedType(typeof(ProfileImageSasTokenRequest), typeDiscriminator: nameof(ProfileImageSasTokenRequest))]
[JsonDerivedType(typeof(RecipePhotoSasTokenRequest), typeDiscriminator: nameof(RecipePhotoSasTokenRequest))]
public class SasTokenRequestBase : RequestBase
{
    
}