namespace HomeCentre.PIM.BAL.Model.Account
{
    public class LoginViewModel
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public string Token { get; set; }

        public string IpAddress { get; set; }
    }
}