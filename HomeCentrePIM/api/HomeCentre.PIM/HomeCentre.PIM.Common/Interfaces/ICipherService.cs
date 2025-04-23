using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface ICipherService
    {
        string Encrypt(string input);

        string Decrypt(string cipherText);

        string Hash(string source);

        bool VerifyHash(string source, string hashed);

        Task<string> GetToken(IUserContext model);
    }
}