using System;
using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model
{
    public class MeModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public List<long> DepartmentId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Mobile { get; set; }

        public string Email { get; set; }

        public string Country { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public List<long> RoleId { get; set; }

        public long GenderId { get; set; }

        public bool IsActive { get; set; }

        public bool Isemailverified { get; set; }

        public bool HasActivationRequest { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset DateModified { get; set; }

        public bool HasProfilePicture { get; set; }

        public string ProfilePictureUrl => $"/image/user?userId={this.Id}&version={this.DateModified.Ticks}";

        public DateTime? LastLoginDate { get; set; }

        public string LastLoginLocation { get; set; }

        public long? TenantId { get; set; }
    }
}