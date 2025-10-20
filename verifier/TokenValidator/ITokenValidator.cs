using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace verifier.TokenValidator
{
    public interface ITokenValidator
    {
        JwtSecurityToken Read(string token);
        void Validate(string token, SecurityKey key, string audience, TimeSpan clockSkew);
    }
}
