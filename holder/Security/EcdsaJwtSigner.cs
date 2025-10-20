using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace holder.Security
{
    public class EcdsaJwtSigner : IJwtSigner
    {
        private readonly ECDsa _ecdsa;
        private readonly string _kidDidJwk;

        public EcdsaJwtSigner()
        {
            _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            _kidDidJwk = BuildDidJwkKid(_ecdsa);
        }

        public string KeyId => _kidDidJwk;

        public string SignJwt(IEnumerable<Claim> claims, string audience, DateTimeOffset iat, DateTimeOffset nbf, DateTimeOffset exp)
        {
            var handler = new JwtSecurityTokenHandler();

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Audience = audience,
                IssuedAt = iat.UtcDateTime,
                NotBefore = nbf.UtcDateTime,
                Expires = exp.UtcDateTime,
                SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(_ecdsa) { KeyId = _kidDidJwk }, SecurityAlgorithms.EcdsaSha256)
            };

            return handler.CreateEncodedJwt(descriptor);
        }

        private string BuildDidJwkKid(ECDsa ecdsa)
        {
            var p = ecdsa.ExportParameters(includePrivateParameters: false);

            static string B64Url(byte[] b) => Base64UrlEncoder.Encode(b);

            var jwkPublic = new
            {
                kty = "EC",
                crv = "P-256",
                x = B64Url(p.Q.X!),
                y = B64Url(p.Q.Y!)
            };

            var json = JsonSerializer.Serialize(jwkPublic);
            var didJwk = "did:jwk:" + Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
            return didJwk;
        }

        public void Dispose() => _ecdsa.Dispose();
    }
}
