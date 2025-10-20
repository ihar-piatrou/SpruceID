using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace verifier.TokenValidator
{
    public class JwtTokenValidator : ITokenValidator
    {
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtSecurityToken Read(string token) => _handler.ReadJwtToken(token);

        public void Validate(string token, SecurityKey key, string audience, TimeSpan clockSkew)
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = clockSkew,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key
            };

            _handler.ValidateToken(token, parameters, out _);
        }
    }
}
