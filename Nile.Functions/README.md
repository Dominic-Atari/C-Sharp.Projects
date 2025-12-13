# Nile Functions Project

## Overview
Azure Functions isolated worker (net8) providing user and post APIs against the Nile database.

## Project Structure
```
Nile.Functions
├── Properties
│   └── launchSettings.json
├── Functions
│   └── HttpTriggerFunction.cs
├── Models
├── Services
├── host.json
├── local.settings.json
├── Program.cs
├── Nile.Functions.csproj
└── README.md
```

## Getting Started

### Prerequisites
- .NET 8 SDK
- Azure Functions Core Tools
- An IDE or text editor (e.g., Visual Studio Code)

### Setup & Run (local)
1. Restore: `dotnet restore Nile.Functions.sln`
2. Configure `Functions/local.settings.json` (do not commit secrets):
   - `SqlServerConnectionString`: your SQL server
   - `ApiKey`: key for `x-api-key` (optional if JWT used)
   - `Jwt:Secret` (HS256 shared secret). Optional; set `Jwt:Issuer/Audience` if you want to validate them.
   - `AzureWebJobsStorage`: `UseDevelopmentStorage=true`
3. Run from `Functions`: `func start`

### Auth
- If `Jwt:Secret` is set: send `Authorization: Bearer <jwt>` (HS256; issuer/audience enforced if provided).
- Else if `ApiKey` is set: send header `x-api-key: <key>`.
- If neither is set: routes are open (local/dev only).

### Endpoints
- Health: `GET /api/health`
- Users: `POST /api/users`, `GET /api/users?skip=&take=`, `PATCH /api/users/{id}`, `DELETE /api/users/{id}`
- Posts: `POST /api/posts`, `GET /api/posts?skip=&take=`, `PATCH /api/posts/{id}`, `DELETE /api/posts/{id}`

Responses are wrapped: `{ "data": ..., "error": { "message", "details" } }` (error null on success).

### CI
GitHub Actions workflow at `.github/workflows/ci.yml` runs restore/build/test on push/PR to `main`.

### Deploying to Azure (manual)
1. Ensure a Function App exists and settings for `SqlServerConnectionString`, `ApiKey` (or JWT), storage, and App Insights are configured.
2. Deploy with Core Tools: `func azure functionapp publish <YourFunctionAppName>`.

## Notes
- ServiceBus/App Insights settings are optional/placeholders; current Functions do not use Service Bus.
- Do not commit real secrets; share `local.settings.template.json` instead.

## Class Libraries Overview

Below is an overview of all class library projects in this solution and the concrete classes they contain. This helps discover where capabilities live and what building blocks are available.

### Accessors/Nile.Accessors
- AccessorBase
- Account/AccountAccessor
- DataContracts/PostData
- DateUtility
- HealthCheck/HealthCheckAccessor
- Mapper
- Paging/PagingTokenBase
- Posts/PostAccessor
- User/UserAccessor

### Managers/Nile.Managers
- Admin/AdminManager (partial; defined across multiple files)
- Admin/HealthCheckEngine
- Engagement/EngagementManager (partial)
- Engagement/ContentEngagementManager (partial)
- Engagement/SocialEngagementManager (partial)
- ManagerBase
- Mapping/ClientDtoMapper
- Mapping/ClientContractsProfile
- Proxys/Proxy<TManager>
- Proxys/ProxyAuthorizer
- Proxys/ProxyConverter
- Proxys/ProxyValidator
- ServiceRegistration

### Managers/Nile.Managers.Contract.Client
- DataContract/RequestBase
- DataContract/ResponseBase
- DataContract/Admin/AdminContextResponse
- DataContract/Admin/LoginRequest
- DataContract/HealthCheck/HealthCheckRequest
- DataContract/HealthCheck/HealthCheckResponse
- DataContract/HealthCheck/TestMessage
- DataContract/V1/PageableRequestBase
- DataContract/V1/PageableResponseBase
- DataContract/V1/Patterns (static)
- DataContract/V1/SasTokenRequestBase
- DataContract/V1/SasTokenResponseBase
- DataContract/V1/SasTokens/OcrSasTokenRequest
- DataContract/V1/SasTokens/PostPhotoSasTokenRequest
- DataContract/V1/SasTokens/ProfileImageSasTokenRequest
- DataContract/V1/SasTokens/RecipePhotoSasTokenRequest
- DataContract/V1/Social/CreatePostRequest
- DataContract/V1/Social/CreatePostResponse
- DataContract/V1/Social/FriendRequestAcceptedEvent
- DataContract/V1/Social/PostEnrichmentRequest
- DataContract/V1/Social/PostEnrichmentResponse
- DataContract/V1/Social/PostModel
- DataContract/V1/Social/UnfriendEvent
- DataContract/V1/User/CreateUserProfileRequest
- DataContract/V1/User/DeleteUserProfileImageRequest
- DataContract/V1/User/FriendListSearchRequest
- DataContract/V1/User/FriendRequestRequest
- DataContract/V1/User/LoginRequest
- DataContract/V1/User/LoginUserProfile
- DataContract/V1/User/ReceivedFriendRequestsRequest
- DataContract/V1/User/SentFriendRequestsRequest
- DataContract/V1/User/StoreNotificationPreferencesRequest
- DataContract/V1/User/StoreUserProfileImageRequest
- DataContract/V1/User/StoreUserRequestBase
- DataContract/V1/User/StoreUserResponseBase
- DataContract/V1/User/UnfriendRequest
- DataContract/V1/User/UpdateFriendRequestRequest
- DataContract/V1/User/UpdateUserProfileRequest
- DataContract/V1/User/UserContextResponse
- DataContract/V1/User/UserProfileRequest
- DataContract/V1/User/UserSearchRequestBase
- DataContract/V1/User/UserSearchResponseBase
- DataContract/V1/User/UserSettings
- DataContract/V1/User/UsernameSuggestionsRequest
- DataContract/V1/User/UsernameSuggestionsResponse

### Clients/Nile.Client.Functions
- Common/ContextTypeAttribute
- FunctionBase
- SocialMediaFeed/SocialMediaFunction
- User/V1/UserFunction

### Common/Nile.Common
- Contexts/ContextBase
- Errors/ErrorBase
- Errors/ConflictError
- Errors/ExternalTimeoutError
- Errors/ForbiddenError
- Errors/InternalError
- Errors/NotFoundError
- Errors/UnauthorizedError
- Errors/ValidationError
- Exceptions/ExceptionBase (abstract)
- Exceptions/ConflictException
- Exceptions/ExternalTimeoutException
- Exceptions/ForbiddenException
- Exceptions/NotFoundException
- Exceptions/ValidationException
- InternalDTOs/AccountRequest
- InternalDTOs/AccountRespnce
- InternalDTOs/AccountResponse
- InternalDTOs/AccountStatusRequest
- InternalDTOs/AccountStatusResponse
- InternalDTOs/ConfigBuilder (static)
- InternalDTOs/ConfigUtility
- InternalDTOs/CreatePostRequest
- InternalDTOs/CreatePostResponse
- InternalDTOs/CreateUserProfileRequest
- InternalDTOs/CreateUserRequest
- InternalDTOs/FeedPostRequestBase
- InternalDTOs/FeedPostResponseBase
- InternalDTOs/LoginAccount
- InternalDTOs/MobileUserContext
- InternalDTOs/PostEnrichmentRequest
- InternalDTOs/PostRequest
- InternalDTOs/PostResponse
- InternalDTOs/RequestBase (abstract)
- InternalDTOs/ResponseBase (abstract)
- InternalDTOs/ServiceContractBase (abstract)
- InternalDTOs/StoreAccountRequest
- InternalDTOs/StoreUserRequestBase
- InternalDTOs/StoreUserResponseBase
- InternalDTOs/UpdatePostExternalIdRequest
- InternalDTOs/UserRequestBase (abstract)
- InternalDTOs/UserResponseBase (abstract)
- InternalDTOs/UserTokenRequest
- ServiceContainerBase (abstract)

### Common/Nile.Client.Abstractions
- No concrete classes (interfaces/abstractions only)

### Data/Nile.Database
- Converters/DateOnlyConverter
- DataContracts/DatabaseContext
- DatabaseContextBase (abstract)
- Entities/Account
- Entities/Comment
- Entities/School
- Entities/EmailConfirmations
- Entities/Like
- Entities/Notification
- Entities/Passwords
- Entities/Post
- Entities/ResetPasswordTokens
- Entities/Role
- Entities/User
- Entities/UserProfile
- Entities/UserRelationship
- Entities/UserRole
- Interceptors/AzureSqlAccessTokenInterceptor

### Data/Nile.DbUp
- DbUpSqlConnection
- Program (internal static)

### Data/Nile.MbUp
- AsyncDelayer
- PostProcessingQueuer
- PostUpdatedRequest
- Program (internal static)
- QueueCreator
- Queues (static)

### Engines/Nile.Engines
- Validation/EngineBase
- Validation/ValidationEngine

### Nile.Utilities
- AzureSdk/AzureSdkUtility (partial class across multiple files)
- AzureSdk/UploadSasInfo
- AzureSdk/UtilityBase (abstract)
- AzureSdk/VideoUploadedEnvelope
- ContextFactoryUtility
- NoOpMessageBusUtility
- NoOpSecurityUtility
- NoOpSocialAuthUtility
- NoOpSocialFeedUtility
- ServiceRegistration