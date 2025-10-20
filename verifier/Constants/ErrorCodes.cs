namespace verifier.Constants
{
    /// <summary>
    /// Constants for error codes returned during verification.
    /// </summary>
    public static class ErrorCodes
    {
        public const string MissingToken = "missing_token";
        public const string InvalidTokenFormat = "invalid_token_format";
        public const string MissingKid = "missing_kid";
        public const string KeyResolutionFailed = "key_resolution_failed";
        public const string AudienceMismatch = "aud_mismatch";
        public const string MissingNonce = "missing_nonce";
        public const string MissingHolderId = "missing_holder_id";
        public const string InvalidNonce = "invalid_nonce";
        public const string NonceUsed = "nonce_used";
        public const string NonceExpired = "nonce_expired";
        public const string MethodMismatch = "method_mismatch";
        public const string PathMismatch = "path_mismatch";
        public const string SignatureInvalidOrExpired = "sig_invalid_or_expired";
    }
}
