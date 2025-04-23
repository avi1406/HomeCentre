namespace HomeCentre.PIM.DAL.Master
{
    public class Currency : MasterEntity
    {
        public string Entity { get; set; }

        public string AlphabeticCode { get; set; }

        public string NumericCode { get; set; }

        public string MinorUnit { get; set; }

        public string WithdrawalDate { get; set; }
    }
}