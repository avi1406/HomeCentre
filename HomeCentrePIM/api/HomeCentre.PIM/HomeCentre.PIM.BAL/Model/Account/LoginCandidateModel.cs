namespace HomeCentre.PIM.BAL.Model.Account
{
    public class LoginCandidateModel
    {
        public string Email { get; set; }

        public string Otp { get; set; }

        public string Hash { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string IpAddress { get; set; }
    }
}