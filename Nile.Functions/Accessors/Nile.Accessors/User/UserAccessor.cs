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
            if (request is DTO.CreateUserProfileRequest profileReq)
            {
                return CreateUserFromProfile(profileReq);
            }
            if (request is DTO.CreateUserRequest legacyReq)
            {
                return CreateUserLegacy(legacyReq);
            }
            throw new NotImplementedException($"{nameof(IUserAccessor.Store)} not implemented for '{request.GetType().Name}' (yet!).");
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