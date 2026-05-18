using Nile.Common;

namespace Nile.Database.Entities
{
    public class Role
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}