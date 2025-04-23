using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class CipherService : ICipherService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IConfiguration _configuration;
        private readonly string _key;

        public CipherService(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _configuration = configuration;
            _key = configuration["Jwt-Secret"];
        }

        public string Encrypt(string input)
        {
            return input.Encrypt(_key);
        }

        public string Decrypt(string cipherText)
        {
            return cipherText.Decrypt(_key);
        }

        public string Hash(string source)
        {
            return BCrypt.Net.BCrypt.HashPassword(source);
        }

        public bool VerifyHash(string source, string hashed)
        {
            return BCrypt.Net.BCrypt.Verify(source, hashed);
        }

        public async Task<string> GetToken(IUserContext model)
        {
            var secret = _configuration["AppSettings:Jwt-Secret"];
            var symmetricKey = Convert.FromBase64String(secret);
            var tokenHandler = new JwtSecurityTokenHandler();

            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, (await model.GetUserId()).ToString()),
                    new Claim(ClaimTypes.Email, await model.GetUserEmail()),
                    new Claim("FullName", await model.GetName()),
                }),

                Expires = now.AddMinutes(Convert.ToInt32(3600)),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(symmetricKey),
                    SecurityAlgorithms.HmacSha256Signature),
                Audience = _configuration["AppSettings:Jwt-Audience"],
                Issuer = _configuration["AppSettings:Jwt-Issuer"]
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);
        }
    }
}