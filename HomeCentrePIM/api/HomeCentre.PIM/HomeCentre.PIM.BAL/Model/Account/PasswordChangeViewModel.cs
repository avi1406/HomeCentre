using System.ComponentModel.DataAnnotations;

namespace HomeCentre.PIM.BAL.Model.Account
{
    public class PasswordChangeViewModel
    {
        [Required]
        public string VerificationKey { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }
    }
}