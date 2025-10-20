namespace verifier.Constants
{
    /// <summary>
    /// Constants for JWT claim names used in verification.
    /// </summary>
    public static class JwtClaimNames
    {
        public const string Nonce = "nonce";
        public const string Subject = "sub";
        public const string HolderId = "holder_id";
        public const string Method = "method";
        public const string Path = "path";
    }
}
