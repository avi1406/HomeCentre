using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.BAL.Model.Tenant
{
    [SearchField(new[] { "Name", "Email", "Mobile" })]
    public class UserListItemModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public bool IsActive { get; set; }

        public bool Ismailverified { get; set; }

        public string Role { get; set; }

        public long? RoleId { get; set; }

        public long? TenantId { get; set; }

        public long? GenderId { get; set; }

        public string? Rolelevel { get; set; }

        public int? Age { get; set; }
    }
}