using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.User.Interface.Model;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor
{
    private async Task<UserStoreResult> Handle(UserStoreRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var now = DateTime.UtcNow;
        var context = ProxyForService<IContextUtility>().GetContext();

        DB.User entity;
        if (request.UserId is { } id)
        {
            entity = await db.Users.SingleOrDefaultAsync(u => u.UserId == id && !u.Deleted)
                     ?? throw new NotFoundException(context, nameof(DB.User), nameof(DB.User.UserId), id);

            entity.FirstName = request.FirstName;
            entity.LastName = request.LastName;
            entity.Email = request.Email;
            entity.Role = (DB.UserRole)request.Role;
            entity.CohortId = request.CohortId;
            entity.ModifiedUtc = now;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                entity.PasswordHash = HashPassword(request.Password);
            }
        }
        else
        {
            var emailTaken = await db.Users
                .AnyAsync(u => u.Email == request.Email && !u.Deleted);
            if (emailTaken)
            {
                throw new ConflictException(context, nameof(DB.User), nameof(DB.User.Email),
                    $"A user with email '{request.Email}' already exists.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ValidationException(context, "Password is required for new users.", nameof(request.Password));
            }

            entity = new DB.User
            {
                UserId = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = (DB.UserRole)request.Role,
                CohortId = request.CohortId,
                CreatedUtc = now,
                ModifiedUtc = now
            };
            db.Users.Add(entity);
        }

        await db.SaveChangesAsync();

        var cohortName = entity.CohortId is null
            ? null
            : await db.Cohorts.Where(c => c.CohortId == entity.CohortId)
                .Select(c => c.Name).SingleOrDefaultAsync();

        return new UserStoreResult
        {
            User = new UserInfo
            {
                UserId = entity.UserId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Email = entity.Email,
                Role = (UserRole)entity.Role,
                CohortId = entity.CohortId,
                CohortName = cohortName,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc
            }
        };
    }

    private static string HashPassword(string password) =>
        Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
}
