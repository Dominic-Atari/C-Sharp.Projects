using AutoMapper;

namespace Nile.Managers.Contract.Client.Mapping;

/// <summary>
/// AutoMapper profile for maps between Client Contracts (CLI) and Common DTOs (DTO).
/// Kept in Contracts.Client to avoid circular references with Managers.
/// </summary>
public class ClientContractsProfile : Profile
{
    public ClientContractsProfile()
    {
        // Map base request types and include all known derived types
        CreateMap<CLI.RequestBase, DTO.RequestBase>()
            .IncludeAllDerived();

        // Map internal response base to client response base
        CreateMap<DTO.ResponseBase, CLI.ResponseBase>()
            .IncludeAllDerived();

        // Social: Post enrichment
        // Client request -> Internal concrete request
        CreateMap<Social.PostEnrichmentRequest, DTO.PostRequest>()
            .ForMember(dest => dest.PostId, opt => opt.MapFrom(src => src.Ids != null && src.Ids.Length > 0 ? src.Ids[0] : Guid.Empty));

        // Also keep base mapping for any generic flows
        CreateMap<Social.PostEnrichmentRequest, DTO.FeedPostRequestBase>()
            .IncludeAllDerived();

        // Internal to client response mapping can be specialized later per shape
        CreateMap<DTO.FeedPostResponseBase, Social.PostEnrichmentResponse>()
            .IncludeAllDerived();

        // Add additional maps here as needed, e.g., specific V1.User DTOs
        // User create: Client -> Internal DTO (use profile-specific DTO so profile fields flow through)
        CreateMap<CLI.V1.User.CreateUserProfileRequest, DTO.CreateUserProfileRequest>();
        CreateMap<CLI.V1.User.UpdateUserProfileRequest, DTO.UpdateUserProfileRequest>();
        CreateMap<CLI.V1.User.StoreUserProfileImageRequest, DTO.StoreUserProfileImageRequest>();
        CreateMap<CLI.V1.User.DeleteUserProfileImageRequest, DTO.DeleteUserProfileImageRequest>();
        CreateMap<CLI.V1.User.StoreNotificationPreferencesRequest, DTO.StoreNotificationPreferencesRequest>();

        // User store response: Internal DTO -> Client
        CreateMap<DTO.StoreUserResponseBase, CLI.V1.User.StoreUserResponseBase>();

        // Social: Create Post (client -> internal)
        CreateMap<Social.CreatePostRequest, DTO.CreatePostRequest>()
            .ForMember(dest => dest.PostIds, opt => opt.MapFrom(src => src.PostId))
            .ForMember(dest => dest.ImageFilename, opt => opt.MapFrom(src => src.ImageFilename))
            .ForMember(dest => dest.Caption, opt => opt.MapFrom(src => src.Caption));
    }
}
