using System.Diagnostics.CodeAnalysis;

namespace HomeCentre.PIM.BAL.Implementations.B2C
{
    [ExcludeFromCodeCoverage]
    public class B2CTokenKeys
    {
        public Key[] Keys { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Key
    {
        public string Kid { get; set; }

        public int Nbf { get; set; }

        public string Use { get; set; }

        public string Kty { get; set; }

        public string E { get; set; }

        public string N { get; set; }
    }
}