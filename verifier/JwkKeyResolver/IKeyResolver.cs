using Microsoft.IdentityModel.Tokens;

namespace verifier.JwkKeyResolver
{
    public interface IKeyResolver
    {
        SecurityKey ResolveFromDid(string didUrl);
    }
}
