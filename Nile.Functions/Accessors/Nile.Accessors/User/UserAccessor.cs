using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nile.Common.Extensions;
using Nile.Database.DataContracts;
using Nile.Utilities;
using Nile.Utilities.AzureSdk;
using EF = Nile.Database.Entities;

namespace Nile.Accessors.User
{
    internal class UserAccessor : AccessorBase, IUserAccessor
    {
        private readonly DatabaseContext _dbContext;
        private readonly IDateUtility _dateUtility;
        private readonly IConfigUtility _configurationUtility;
        private readonly IMapper _mapper;
        private readonly IBlobStorageUtility _blobStorageUtility;

        public UserAccessor(
            ILogger<UserAccessor> logger,
            DatabaseContext dbContext,
            IDateUtility dateUtility,
            IConfigUtility configurationUtility,
            IMapper mapper,
            IBlobStorageUtility blobStorageUtility) : base(logger)
        {
            _dbContext = dbContext;
            _dateUtility = dateUtility;
            _configurationUtility = configurationUtility;
            _mapper = mapper;
            _blobStorageUtility = blobStorageUtility;
        }

        Task<DTO.UserResponseBase> IUserAccessor.Store(DTO.UserRequestBase request)
        {
            return request switch
            {
                DTO.CreateUserProfileRequest profileReq => CreateUserFromProfile(profileReq),
                DTO.CreateUserRequest legacyReq => CreateUserLegacy(legacyReq),
                DTO.UpdateUserProfileRequest updateReq => UpdateUserProfile(updateReq),
                DTO.StoreUserProfileImageRequest imgReq => StoreProfileImage(imgReq),
                DTO.DeleteUserProfileImageRequest delImgReq => DeleteProfileImage(delImgReq),
                DTO.StoreNotificationPreferencesRequest prefsReq => StoreNotificationPreferences(prefsReq),
                _ => throw new NotImplementedException($"{nameof(IUserAccessor.Store)} not implemented for '{request.GetType().Name}' (yet!).")
            };
        }

        private async Task<DTO.UserResponseBase> UpdateUserProfile(DTO.UpdateUserProfileRequest req)
        {
            var profile = await _dbContext.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Username == req.Username);

            if (profile == null)
            {
                throw new InvalidOperationException($"User profile not found for username '{req.Username}'.");
            }

            profile.FirstName = req.FirstName;
            profile.LastName = req.LastName;
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<DTO.StoreUserResponseBase>(profile.User);
        }

        private async Task<DTO.UserResponseBase> StoreProfileImage(DTO.StoreUserProfileImageRequest req)
        {
            // The request DTO carries only ImageFilename today; callers are expected to identify
            // the target user via the auth context. Until that flow is wired through, this method
            // operates against the most-recently-created profile as a placeholder.
            var profile = await _dbContext.UserProfiles
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                throw new InvalidOperationException("No user profile found to attach the image to.");
            }

            profile.ProfileImageFilename = req.ImageFilename;
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<DTO.StoreUserResponseBase>(profile.User);
        }

        private async Task<DTO.UserResponseBase> DeleteProfileImage(DTO.DeleteUserProfileImageRequest req)
        {
            var profile = await _dbContext.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == req.UserId);

            if (profile == null)
            {
                throw new InvalidOperationException($"User profile not found for user '{req.UserId}'.");
            }

            profile.ProfileImageFilename = null;
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<DTO.StoreUserResponseBase>(profile.User);
        }

        private async Task<DTO.UserResponseBase> StoreNotificationPreferences(DTO.StoreNotificationPreferencesRequest req)
        {
            // Same caveat as StoreProfileImage: the request has no UserId yet, so we target the
            // most recently created profile until the auth-context plumbing lands.
            var profile = await _dbContext.UserProfiles
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (profile == null)
            {
                throw new InvalidOperationException("No user profile found to attach preferences to.");
            }

            profile.NotificationPreferencesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                req.NotifyOnFriendRequestReceived,
                req.NotifyOnFriendRequestApproved,
                req.NotifyOnPostCommentReceived
            });
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<DTO.StoreUserResponseBase>(profile.User);
        }

        // Legacy path kept for backward compatibility if used elsewhere
        private async Task<DTO.UserResponseBase> CreateUserLegacy(DTO.CreateUserRequest req)
        {
            var now = _dateUtility.UtcNow;
            var user = new EF.User
            {
                Id = Guid.NewGuid(),
                Username = req.EmailAddress, // Fallback: use email as username in legacy path
                CreatedAt = new DateTimeOffset(now, TimeSpan.Zero)
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<DTO.StoreUserResponseBase>(user);
        }

        private async Task<DTO.UserResponseBase> CreateUserFromProfile(DTO.CreateUserProfileRequest req)
        {
            var now = _dateUtility.UtcNow;
            var createdAt = new DateTimeOffset(now, TimeSpan.Zero);

            var user = new EF.User
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                CreatedAt = createdAt
            };

            var profile = new EF.UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = req.FirstName,
                LastName = req.LastName,
                Location = req.Location,
                Bio = null
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.UserProfiles.AddAsync(profile);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<DTO.StoreUserResponseBase>(user);
        }
    }
}