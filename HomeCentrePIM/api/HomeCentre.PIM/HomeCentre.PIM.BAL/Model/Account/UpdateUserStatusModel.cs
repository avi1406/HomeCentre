namespace HomeCentre.PIM.BAL.Model.Account
{
    public class UpdateUserStatusModel
    {
        public long UserId { get; set; }

        public bool IsActive { get; set; }
    }
}