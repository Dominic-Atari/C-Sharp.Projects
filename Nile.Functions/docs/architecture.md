# Solution Architecture (Code Inventory)

This section documents the current solution architecture and provides an exhaustive inventory of authored C# classes/interfaces by project. Generated sources and compiled artifacts under `bin/` and `obj/` are intentionally excluded.

## High-Level Layers

- Clients (Azure Functions HTTP triggers) → Managers (application orchestration) → Accessors (infrastructure/data access) → Data (EF Core entities/contexts, migrations, message bus helpers)
- Common (shared DTOs, errors, exceptions, context abstractions)
- Engines (cross-cutting engines like validation)
- Utilities (public utility contracts and DI bootstrapping)

Typical request flow:
1. HTTP request hits a Function endpoint under `Clients/Nile.Client.Functions` or `Functions`.
2. The function composes a request context and validates inputs; it calls a Manager interface.
3. The Manager orchestrates Accessors (users/posts) and Utilities (blob, message bus, social, time, mapping).
4. Accessors use EF Core `DatabaseContext` and entities and may call Azure SDK utilities.
5. Results are mapped to DTOs and returned to the client.

Dependency Injection:
- Central bootstrap: `Nile.Utilities/ServiceRegistration.cs`
  - Binds `AzureSdk.IDateUtility` → `Nile.Accessors.DateUtility` (singleton)
  - Registers `Nile.Accessors.Mapper` and internal abstraction `Nile.Accessors.IMapper`
  - Exposes `AutoMapper.IConfigurationProvider` from `Mapper.Configuration`
  - Accessors:
    - `Nile.Accessors.User.IUserAccessor` → `Nile.Accessors.User.UserAccessor` (scoped)
    - `Nile.Accessors.Posts.IPostAccessor` → `Nile.Accessors.Posts.PostAccessor` (scoped)
- Function hosts (`Clients/.../Program.cs` and `Functions/Program.cs`) import these registrations.

---

## Project Inventory and Classes

Below is an exhaustive list of authored `.cs` sources per project/folder.

### Clients — Azure Functions
Path: `Clients/Nile.Client.Functions`

- `Properties/AssemblyInfo.cs`
- `Common/Context.cs` — `Context`
- `FunctionBase.cs`
- `Program.cs`
- `SocialMediaFeed/SocialMediaFunction.cs`
- `User/V1/UserFunction.cs`

Primary usage: Entry points depending on Manager interfaces, Common DTOs, and Utilities. No direct DB access.

---

### Functions — Host-level Entry
Path: `Functions`

- `HttpTriggerFunction.cs`
- `Program.cs`

Primary usage: Companion Azure Functions host; composes DI and configuration.

---

### Managers — Contracts and Orchestrators
Path: `Managers/Nile.Managers.Contract.Client`

Engagement:
- `Engagement/IContentEngagementManager.cs`
- `Engagement/ISocialEngagementManager.cs`
- `Engagement/ContentEngagementManager.cs`
- `Engagement/EngagementManager.cs`
- `Engagement/SocialEngagementManager.cs`

Admin & User:
- `Admin/IFileManager.cs`
- `Admin/IHealthCheckManager.cs`
- `Admin/IUserManager.cs`
- `Admin/FilterManager.cs`

Mapping:
- `Mapping/ClientDtoMapper.cs`
- `Mapping/DtoMapper.cs`

Plumbing Contracts:
- `Proxys/IProxy.cs`, `Proxys/Proxy.cs`
- `Proxys/IAuthorizer.cs`, `Proxys/IConverter.cs`, `Proxys/IValidator.cs`

Paging/Responses:
- `PageableResponseBase.cs`

Client Data Contracts:
- `DataContract/RequestBase.cs`, `DataContract/ResponseBase.cs`
- `DataContract/HealthCheck/HealthCheckRequest.cs`, `HealthCheckResponse.cs`, `TestMessage.cs`
- `DataContract/V1/Patterns.cs`
- `DataContract/V1/SasTokens/*`: `OcrSasTokenRequest.cs`, `PostPhotoSasTokenRequest.cs`, `ProfileImageSasTokenRequest.cs`, `RecipePhotoSasTokenRequest.cs`, plus bases `SasTokenRequestBase.cs`, `SasTokenResponseBase.cs`
- `DataContract/V1/Social/*`: `FriendRequestAcceptedEvent.cs`, `PostEnrichmentRequest.cs`, `PostEnrichmentResponse.cs`, `UnfriendEvent.cs`
- `DataContract/V1/User/*`: `CreateUserProfileRequest.cs`, `DeleteUserProfileImageRequest.cs`, `FriendListSearchRequest.cs`, `FriendRequestRequest.cs`, `LoginRequest.cs`, `LoginUserProfile.cs`, `ReceivedFriendRequestsRequest.cs`, `SentFriendRequestsRequest.cs`, `StoreNotificationPreferencesRequest.cs`, `StoreUserProfileImageRequest.cs`, `StoreUserRequestBase.cs`, `StoreUserResponseBase.cs`, `UnfriendRequest.cs`, `UpdateFriendRequestRequest.cs`, `UpdateUserProfileRequest.cs`, `UserContextResponse.cs`, `UserProfileRequest.cs`, `UserSearchRequestBase.cs`, `UserSearchResponseBase.cs`, `UserSettings.cs`, `UsernameSuggestionsRequest.cs`, `UsernameSuggestionsResponse.cs`

Path: `Managers/Nile.Managers`
- `ManagerBase.cs`
- `DtoMapper.cs`
- `Class1.cs` (placeholder)
- `Properties/AssemblyInfo.cs`

---

### Accessors — Infrastructure & Data Access
Path: `Accessors/Nile.Accessors`

Core and Mapping:
- `AccessorBase.cs`
- `IMapper.cs`, `Mapper.cs`

Azure/Date:
- `DateUtility.cs`

Paging:
- `Paging/PagingTokenBase.cs`

Posts:
- `Posts/IPostAccessor.cs`, `Posts/PostAccessor.cs`

Users:
- `User/IUserAccessor.cs`, `User/UserAccessor.cs`

Internal Data:
- `DataContracts/PostData.cs`

---

### Data — EF Core & Ops
Path: `Data/Nile.Database`

Contexts/Base:
- `DataContracts/DatabaseContext.cs`
- `DatabaseContextBase.cs`

Converters:
- `Converters/DateOnlyConverter.cs`

Interceptors:
- `Interceptors/AzureSqlAccessTokenInterceptor.cs`

Entities:
- `Entities/Account.cs`, `Entities/Comment.cs`, `Entities/CreateSchool.cs`, `Entities/EmailConfirmations.cs`, `Entities/Like.cs`, `Entities/Notification.cs`, `Entities/Passwords.cs`, `Entities/Post.cs`, `Entities/ResetPasswordTokens.cs`, `Entities/Role.cs`, `Entities/User.cs`, `Entities/UserRelationship.cs`, `Entities/UserRelationshipType.cs`, `Entities/UserRole.cs`

Path: `Data/Nile.DbUp`
- `Program.cs`
- `DbUpSqlConnection.cs`
- `Scripts/001/001.CreateDatabase.sql`
- `Scripts/001/003.rolesCreated.sql`

Path: `Data/Nile.MbUp`
- `Program.cs`
- `AsyncDelayer.cs`, `IAsyncDelayer.cs`
- `PostProcessingQueuer.cs`, `PostUpdatedRequest.cs`
- `QueueCreator.cs`, `Queues.cs`

---

### Common — Shared DTOs, Contexts, Errors
Path: `Common/Nile.Common`

Contexts & Containers:
- `Contexts/ContextBase.cs`
- `ServiceContainerBase.cs`

Internal DTOs:
- `InternalDTOs/ConfigBuilder.cs`, `ConfigUtility.cs`, `IConfigUtility.cs`, `CreateUserRequest.cs`, `FeedPostRequestBase.cs`, `FeedPostResponseBase.cs`, `FriendRequestActionType.cs`, `HealthStatusType.cs`, `PostRequest.cs`, `PostResponse.cs`, `RequestBase.cs`, `ResponseBase.cs`, `ServiceContractBase.cs`, `StoreUserRequestBase.cs`, `StoreUserResponseBase.cs`, `UserRequestBase.cs`, `UserResponseBase.cs`, `UserTokenRequest.cs`

Errors & Exceptions:
- `Errors/ErrorBase.cs`, `ConflictError.cs`, `ForbiddenError.cs`, `InternalError.cs`, `NotFoundError.cs`, `UnauthorizedError.cs`, `ValidationError.cs`
- `Exceptions/ExceptionBase.cs`, `ConflictException.cs`, `ExternalTimeoutException.cs`, `ForbiddenException.cs`, `NotFoundException.cs`, `ValidationException.cs`

---

### Engines — Cross-cutting Engines
Path: `Engines/Nile.Engines`

- `Validation/IValidationEngine.cs`

---

### Utilities — Public Utilities and DI
Path: `Nile.Utilities`

Contracts:
- `AzureSdk/IBlobStorageUtility.cs`, `IDateUtility.cs`, `IPhotoContentService.cs`, `IVideoContentService.cs`
- `IContextFactoryUtility.cs`, `IMessageBusUtility.cs`, `ISocialAuthUtility.cs`, `ISocialFeedUtility.cs`

Implementations & Helpers:
- `AzureSdk/AzureSdkUtility.cs`, `BlobStorageUtility.cs`, `PhotoContentService.cs`, `VideoContentService.cs`, `UploadSasInfo.cs`, `VideoUploadedEnvelope.cs`, `UtilityBase.cs`
- `MessageBusUtlility.cs`

DI & Internals Visibility:
- `ServiceRegistration.cs`
- `Properties/InternalsVisibleTo.Functions.cs`

---

### Cross-Project Usage Map

- Clients (Functions)
  - Uses: Managers (interfaces), Common DTOs, Utilities (contracts), `Context`
- Managers.Contract.Client
  - Uses: Accessors (IPostAccessor, IUserAccessor), Utilities (message bus, blob, social, time), Common (DTOs/errors), Engines (validation)
- Managers (base)
  - Uses: Common (base classes, mapping)
- Accessors
  - Uses: Data (DbContext, Entities), Utilities (Azure SDK abstractions), Common (errors), AutoMapper
- Data.Database
  - Used by: Accessors; defines EF model and DB concerns
- Data.DbUp
  - Used for deployment migrations; no runtime coupling
- Data.MbUp
  - Used by Managers/Functions via message bus utilities (queue names and enqueuer abstractions)
- Utilities
  - Used by: Functions and Managers; registers Accessors implementations and public utility services
- Engines
  - Used by: Managers (validation)

---

### Notable Patterns

- Reflection-based DI in `Nile.Utilities.ServiceRegistration` binds internal implementations from `Nile.Accessors` by full type name.
- Contracts-vs-implementations separation: Managers call Accessor interfaces; concretes live in Accessors.
- EF Core with Azure SQL access token interceptor.
- AutoMapper wrapped by internal `Nile.Accessors.IMapper`; `IConfigurationProvider` exposed for `ProjectTo`.
- Azure Functions isolated worker with `Program.cs` startup composition.

---

### Completeness Notes

- The lists above include all authored `.cs` files found under: `Accessors`, `Clients`, `Common`, `Managers` (both), `Data` (Database, DbUp, MbUp), `Engines`, `Utilities`, and `Functions`.
- Generated files in `obj/` and `bin/` are excluded by design.
- Additional directories:
  - `Models/`: no `.cs` detected in the provided index; likely empty or reserved for future POCOs.
  - `NileDb/`, `Services/`: no `.cs` detected in the provided index bounds; can be scanned and appended upon request.

---

# Nile Platform Domain Draft

> Goal: social + learning platform for schools (tenants) with tutors/students accessing courses, lessons (video/notes/exercises), and a moderated social feed. This is a first-pass contract to guide vertical slice development.

## Domain Slices
- **Tenancy & Identity**: Schools as tenants; users belong to one or more schools. Roles: `SchoolAdmin`, `Tutor`, `Student`, `Guardian`, `Support`. Policy-based authorization and per-tenant data isolation.
- **User Profiles**: Core profile + school-specific roles/metadata; verification for tutors; optional guardianship links.
- **Courses & Classes**: Course catalog per school, classes/sections per term; enrollment roster; prerequisites.
- **Lessons & Content**: Lessons hold narrative, attachments, video streams, and exercises. Versioned content with draft/publish states.
- **Assignments & Quizzes**: Assignable to class or individual; due dates, submission attempts, grading, feedback; rubric optional.
- **Media & Storage**: Video and large files stored in blob/CDN; signed URLs; background processing for transcodes/thumbnails.
- **Social Feed**: Posts, comments, reactions; scoped to school/class groups; moderation (flags, mutes, blocks); rate limits.
- **Notifications**: In-app + email/push; templates; delivery preferences; throttling.
- **Analytics & Progress**: Lesson completion, time-on-task, quiz scores, tutor activity; export/reporting.

## Entities and Properties
- `School`
	- Id (guid), Name, Slug, Domain, SettingsJson (moderation, upload limits), BillingStatus, CreatedAt, UpdatedAt
- `User`
	- Id (guid), Email, DisplayName, AvatarUrl, PrimaryRole, Roles[], Status, CreatedAt, LastLoginAt
- `TenantMembership`
	- Id, UserId, SchoolId, Roles[], Status (Pending|Active|Suspended), InvitedBy, JoinedAt
- `GuardianLink`
	- Id, StudentUserId, GuardianUserId, Status, ConsentAt
- `Course`
	- Id, SchoolId, Title, Summary, Subject, Level, Visibility (Private|School|Public), OwnerId, Tags[], PublishedAt, CreatedAt, UpdatedAt
- `ClassSection`
	- Id, CourseId, Term, Name, ScheduleJson, RosterSize, InviteCode, StartsAt, EndsAt, CreatedAt, UpdatedAt
- `Enrollment`
	- Id, ClassSectionId, UserId, Role (Tutor|Student), Status, EnrolledAt, DroppedAt
- `Lesson`
	- Id, CourseId, ClassSectionId?, Title, SequenceNo, ContentFormat (Markdown|Json|Html), ContentBody, Attachments[], DurationMinutes, Status (Draft|Published), Version, PublishedAt, CreatedAt, UpdatedAt
- `MediaAsset`
	- Id, SchoolId, OwnerId, Type (Video|Doc|Image|Audio), StorageUri, ContentLength, ContentType, ThumbnailUri, SignedUrlTtlSeconds, ProcessingStatus, CreatedAt, UpdatedAt
- `Assignment`
	- Id, CourseId, ClassSectionId?, Title, Description, DueAt, MaxPoints, SubmissionType (File|Text|Quiz), RubricJson, Visibility, CreatedAt, UpdatedAt
- `Submission`
	- Id, AssignmentId, UserId, SubmittedAt, ContentUri, AnswersJson, Grade, GradedBy, Feedback, AttemptNo, Status (Draft|Submitted|Graded|Returned), CreatedAt, UpdatedAt
- `Quiz`
	- Id, CourseId, Title, QuestionsJson (versioned), ScoringRulesJson, TimeLimitSeconds, AttemptsAllowed, CreatedAt, UpdatedAt
- `QuizAttempt`
	- Id, QuizId, UserId, StartedAt, CompletedAt, AnswersJson, Score, Status, AttemptNo
- `FeedPost`
	- Id, SchoolId, Scope (School|ClassSection|Direct), ScopeRefId, AuthorId, Body, Attachments[], Visibility, CreatedAt, UpdatedAt
- `Comment`
	- Id, PostId, AuthorId, Body, CreatedAt, ParentCommentId?, UpdatedAt
- `Reaction`
	- Id, TargetType (Post|Comment), TargetId, UserId, Type (Like|Celebrate|Insightful|Support|Custom), CreatedAt
- `Notification`
	- Id, UserId, Type, PayloadJson, Channel (InApp|Email|Push), Status (Pending|Sent|Failed|Read), SentAt, ReadAt, CreatedAt
- `AuditEvent`
	- Id, ActorId, SchoolId, Action, TargetRef, MetadataJson, CreatedAt, CorrelationId
- `RateLimitCounter`
	- Id, UserId, Scope, WindowStart, WindowEnd, Count
- `FeatureFlag`
	- Id, Key, Audience (All|School|User), ValueJson, Enabled, CreatedAt, UpdatedAt

## API Surface (outline)
- Auth: `POST /auth/login`, `POST /auth/refresh`, `POST /auth/verify`, `GET /me`.
- Schools: `POST /schools`, `GET /schools/{id}`, `PATCH /schools/{id}`, `POST /schools/{id}/members` (invite/add), `GET /schools/{id}/members`.
- Courses: `POST /schools/{id}/courses`, `GET /schools/{id}/courses`, `GET /courses/{id}`, `PATCH /courses/{id}`.
- Classes: `POST /courses/{id}/sections`, `GET /courses/{id}/sections`, `POST /sections/{id}/enroll`, `POST /sections/{id}/roster`.
- Lessons & Content: `POST /courses/{id}/lessons`, `GET /courses/{id}/lessons`, `GET /lessons/{id}`, `PATCH /lessons/{id}`, `POST /lessons/{id}/publish`.
- Media: `POST /media` (request upload, returns signed URL), `GET /media/{id}` (signed playback URL), `POST /media/{id}/complete` (mark upload done).
- Assignments/Quizzes: `POST /courses/{id}/assignments`, `GET /courses/{id}/assignments`, `POST /assignments/{id}/submit`, `GET /assignments/{id}/submissions`, `POST /assignments/{id}/grade`.
- Feed: `POST /feed`, `GET /feed?scope=school|section`, `POST /feed/{id}/comments`, `POST /feed/{id}/reactions`.
- Notifications: `GET /notifications`, `PATCH /notifications/{id}` (mark read), `POST /notifications/test` (admin).
- Admin/Ops: `GET /health`, `GET /metrics` (secured), `GET /audit` (role-gated).

## Eventing & Async
- Queue events for media processing (`MediaUploaded` -> transcode -> `MediaReady`).
- Notifications pipeline (enqueue, template render, dispatch via email/push/in-app).
- Feed enrichment or fan-out jobs; rate limiters on post/comment endpoints.
- Audit log writes should be fire-and-forget via queue where possible.

## Data & Storage Plan
- Relational DB (SQL) for core entities; migrations managed via DbUp.
- Blob storage + CDN for media/attachments; signed URLs for upload/download.
- Cache (Redis) for session tokens, feed cursors, rate limiting, and feature flags.
- Search (optional later) for courses/feed using Azure Cognitive Search/Elastic.

## Security & Compliance
- Tenant isolation on every query; enforce `SchoolId` scoping in data access.
- Auth: OAuth2/OIDC for SSO (e.g., Microsoft/Google), MFA optional; JWT access + refresh tokens.
- RBAC policies per endpoint; validate ownership for content/grades.
- PII protection and least-privilege secrets (Key Vault); never store raw passwords or secrets in repo.
- Content moderation hooks; abuse/report workflow.

## Observability & Quality
- Structured logging + correlation IDs; traces/metrics via OpenTelemetry -> App Insights.
- Health checks for DB, storage, queue, cache.
- Tests: unit for domain/services; integration for DB/Functions; contract tests for endpoints; load tests for media/feeds.
- Feature flags for risky rollouts.

## Initial Vertical Slices (suggested order)
1) Auth + multi-tenant school onboarding + user roles.
2) Course + class section creation + enrollment.
3) Lesson authoring + media upload (signed URL) + publish.
4) Assignments + submissions + grading feedback.
5) Social feed (school scope) + comments + moderation basics.
6) Notifications pipeline for assignments/feed events.

## Open Questions
- Which identity providers are required (school SSO vs. consumer)?
- Do we need guardians/parents and consent flows? Age gating?
- Compliance targets (FERPA/GDPR equivalents) and data residency constraints?
- Video hosting preference (self-managed blob vs. specialized provider)?
- Mobile clients planned now or later? Dictates API surface and auth flows.



//*****************************************************************************************

## Current Repository Layout (what exists today)
- `Functions/` — Azure Functions isolated worker host.
	- `Program.cs` wires configuration, Application Insights, accessors via `AddNileServices`, Azure storage utilities, and generic manager proxy DI.
	- `HttpTriggerFunction.cs` sample HTTP trigger.
	- `host.json`, `local.settings.json` host config; `Properties/launchSettings.json` local debug.
- `Accessors/Nile.Accessors/` — data access helpers and mapping utilities.
	- `AccessorBase.cs` paging token helpers; `DateUtility`, `Mapper`, `ProjectReferenceConfig.xml`; `Posts/`, `User/` accessors; `Paging/` helpers; interfaces like `IMapper`.
- `Managers/` — business logic layer.
	- `Nile.Managers/` (implementation scaffold) and `Nile.Managers.Contract.Client/` (contracts like `DataContract/HealthCheck/TestMessage.cs`).
- `Common/Nile.Common/` — shared abstractions and error/DTO infrastructure.
	- `ServiceContainerBase.cs`, contexts, errors, exceptions, internal DTOs.
- `Engines/Nile.Engines/` — processing engines (structure present; details omitted here).
- `Clients/Nile.Client.Functions/` — client-facing Functions host and shared common components for clients.
	- `FunctionBase.cs`, `Program.cs`, `host.json`, `local.settings.json`, `Common/`, `SocialMediaFeed/`, `User/` folders.
- `Data/`
	- `Nile.Database/` — EF/DB context base and converters/entities/interceptors.
	- `Nile.DbUp/` — DbUp console for migrations (`Program.cs`, scripts folder, local settings).
	- `Nile.MbUp/` — message-bus/updater utilities (async delayer, post-processing queuer, requests).
- `Nile.Utilities/` — utilities + DI registration (`ServiceRegistration.cs`), Azure SDK abstractions (`IContextFactoryUtility`, `IMessageBusUtility`, `ISocialAuthUtility`, `ISocialFeedUtility`), and `MessageBusUtlility.cs`.
- `NileDb/` — SQL project (`NileDb.sqlproj`).
- `Directory.Build.props`, `global.json` — solution-wide SDK/version settings.
- `README.md`, `README.Docker.md` — project overview and Docker setup.
