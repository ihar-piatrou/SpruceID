using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace verifier.JwkKeyResolver
{
    public class DidJwkKeyResolver : IKeyResolver
    {
        public SecurityKey ResolveFromDid(string didUrl)
        {
            // did:jwk:<base64url(UTF8(JWK-JSON))>
            const string prefix = "did:jwk:";
            if (!didUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new SecurityTokenInvalidSigningKeyException("Unsupported DID method (expected did:jwk)");

            var b64 = didUrl.Substring(prefix.Length);
            var jwkJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(b64));

            using var doc = JsonDocument.Parse(jwkJson);
            var jwk = doc.RootElement;

            var kty = jwk.GetProperty("kty").GetString();
            if (kty != "EC") throw new SecurityTokenInvalidSigningKeyException("Only EC JWK supported here");

            var crv = jwk.GetProperty("crv").GetString();
            if (crv is not "P-256") throw new SecurityTokenInvalidSigningKeyException("Only P-256 supported here");

            var x = jwk.GetProperty("x").GetString();
            var y = jwk.GetProperty("y").GetString();

            var xBytes = Base64UrlEncoder.DecodeBytes(x);
            var yBytes = Base64UrlEncoder.DecodeBytes(y);

            var ecParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = xBytes, Y = yBytes }
            };

            var ecdsa = ECDsa.Create(ecParams);
            return new ECDsaSecurityKey(ecdsa);
        }
    }
}