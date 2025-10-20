using System.Security.Claims;

namespace holder.Security
{
    public interface IJwtSigner : IDisposable
    {
        string SignJwt(IEnumerable<Claim> claims, string audience, DateTimeOffset iat, DateTimeOffset nbf, DateTimeOffset exp);
        string KeyId { get; }
    }
}
