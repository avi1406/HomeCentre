namespace HomeCentre.PIM.BAL.Model.App
{
    public class AppConfiguration
    {
        public string ApiUrl { get; set; }

        public string AppUrl { get; set; }

        public B2CConfiguration B2C { get; set; }
    }

    public class B2CConfiguration
    {
        public string LoginDomain { get; set; }

        public string Tenant { get; set; }

        public string LoginFlow { get; set; }

        public string ClientId { get; set; }

        public string ResetPasswordFlow { get; set; }
    }
}