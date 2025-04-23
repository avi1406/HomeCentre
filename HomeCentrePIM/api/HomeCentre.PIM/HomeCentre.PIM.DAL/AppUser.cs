using System;
using System.ComponentModel.DataAnnotations;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL
{
    [SearchField(new[] {"Address", "Domain", "FirstName", "Email", "Name", "LastName", "EmployeeNo", "Mobile"})]
    public class AppUser : Entity
    {
        public string Address { get; set; }

        public string B2CObjectId { get; set; }

        public long? DepartmentId { get; set; }

        public string Domain { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Required(ErrorMessage = "Please input your E-mail!")]
        public string Email { get; set; }

        public string EncryptionKey { get; set; }

        public string FirstName { get; set; }

        public long? GenderId { get; set; }

        public bool HasAcceptedPrivacyPolicy { get; set; }

        public bool HasAcceptedTerms { get; set; }

        public bool IsDeleted { get; set; }

        public bool Isemailverified { get; set; }

        public string LastName { get; set; }

        public string Message { get; set; }

        [StringLength(12, MinimumLength = 4)]
        [Required(ErrorMessage = "Please input phone number!")]
        public string Mobile { get; set; }

        [StringLength(100, MinimumLength = 2)]
        [Required(ErrorMessage = "Please input name!")]
        public string Name { get; set; }

        public string Password { get; set; }

        public string ProfilePicture { get; set; }

        public string VerificationKey { get; set; }

        public string EmployeeNo { get; set; }

        public string? Rolelevel { get; set; }

        public DateTimeOffset VerificationKeyExpiryTime { get; set; }
    }
}