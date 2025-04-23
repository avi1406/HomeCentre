namespace HomeCentre.PIM.BAL.Model.Account
{
    public class AddOrUpdateUserViewModel
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }

        public long Id { get; set; }

        public string ProfilePicture { get; set; }

        public long DepartmentId { get; set; }

        public long RoleId { get; set; }
    }
}