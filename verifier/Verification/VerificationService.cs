using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using verifier.Constants;
using verifier.JwkKeyResolver;
using verifier.Models;
using verifier.NonceStore;
using verifier.Options;
using verifier.SystemTime;
using verifier.TokenValidator;

namespace verifier.Verification
{
    public class VerificationService : IVerificationService
    {
        private readonly ITokenValidator _tokenValidator;
        private readonly IKeyResolver _keyResolver;
        private readonly INonceStore _nonces;
        private readonly ITimeProvider _time;
        private readonly VerifierOptions _opts;

        public VerificationService(
            ITokenValidator tokenValidator,
            IKeyResolver keyResolver,
            INonceStore nonces,
            ITimeProvider time,
            IOptions<VerifierOptions> options)
        {
            _tokenValidator = tokenValidator;
            _keyResolver = keyResolver;
            _nonces = nonces;
            _time = time;
            _opts = options.Value;
        }

        public async Task<VerifyOutcome> VerifyAsync(VerifyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.token))
                return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MissingToken);

            var jwtResult = TryReadJwt(req.token);
            if (!jwtResult.IsSuccess)
                return jwtResult.Error;

            var jwt = jwtResult.Value;

            var keyResult = TryResolveKey(jwt);
            if (!keyResult.IsSuccess)
                return keyResult.Error;

            var publicKey = keyResult.Value;

            var claimsResult = TryExtractClaims(jwt);
            if (!claimsResult.IsSuccess)
                return claimsResult.Error;

            var claims = claimsResult.Value;

            var nonceError = await ValidateNonceAsync(claims.Nonce);
            if (nonceError is not null)
                return nonceError;

            var bindingError = ValidateBinding(claims.Method, claims.Path);
            if (bindingError is not null)
                return bindingError;

            var signatureResult = TryValidateSignature(req.token, publicKey);
            if (!signatureResult.IsSuccess)
                return signatureResult.Error;

            await _nonces.MarkUsedAsync(claims.Nonce);

            var response = new VerificationSuccessResponse(
                Status: "valid",
                HolderId: claims.HolderId,
                Kid: jwt.Header.Kid ?? string.Empty,
                VerifiedAt: _time.UtcNow);

            return VerifyOutcome.Ok(response);
        }

        private OperationResult<JwtSecurityToken> TryReadJwt(string token)
        {
            try
            {
                var jwt = _tokenValidator.Read(token);
                return OperationResult<JwtSecurityToken>.Success(jwt);
            }
            catch (Exception ex)
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.InvalidTokenFormat, ex.Message);
                return OperationResult<JwtSecurityToken>.Failure(error);
            }
        }

        private OperationResult<SecurityKey> TryResolveKey(JwtSecurityToken jwt)
        {
            var kid = jwt.Header.Kid;
            if (string.IsNullOrWhiteSpace(kid))
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MissingKid);
                return OperationResult<SecurityKey>.Failure(error);
            }

            try
            {
                var key = _keyResolver.ResolveFromDid(kid);
                return OperationResult<SecurityKey>.Success(key);
            }
            catch (Exception ex)
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.KeyResolutionFailed, ex.Message);
                return OperationResult<SecurityKey>.Failure(error);
            }
        }

        private readonly record struct RequiredClaims(
            string Audience,
            string Nonce,
            string HolderId,
            string? Method,
            string? Path);

        private OperationResult<RequiredClaims> TryExtractClaims(JwtSecurityToken jwt)
        {
            var aud = jwt.Audiences.FirstOrDefault();
            var nonce = jwt.Claims.FirstOrDefault(c => c.Type == JwtClaimNames.Nonce)?.Value;
            var holderId = jwt.Claims.FirstOrDefault(c => c.Type is JwtClaimNames.Subject or JwtClaimNames.HolderId)?.Value;
            var method = jwt.Claims.FirstOrDefault(c => c.Type == JwtClaimNames.Method)?.Value;
            var path = jwt.Claims.FirstOrDefault(c => c.Type == JwtClaimNames.Path)?.Value;

            if (aud != _opts.Audience)
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.AudienceMismatch);
                return OperationResult<RequiredClaims>.Failure(error);
            }

            if (string.IsNullOrEmpty(nonce))
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MissingNonce);
                return OperationResult<RequiredClaims>.Failure(error);
            }

            if (string.IsNullOrEmpty(holderId))
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MissingHolderId);
                return OperationResult<RequiredClaims>.Failure(error);
            }

            var claims = new RequiredClaims(aud!, nonce!, holderId!, method, path);
            return OperationResult<RequiredClaims>.Success(claims);
        }

        private async Task<VerifyOutcome?> ValidateNonceAsync(string nonce)
        {
            var (found, rec) = await _nonces.TryGetAsync(nonce);
            if (!found) return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.InvalidNonce);
            if (rec.Used) return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.NonceUsed);
            if (_time.UtcNow > rec.ExpiresAt) return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.NonceExpired);
            return null;
        }

        private VerifyOutcome? ValidateBinding(string? method, string? path)
        {
            if (!string.Equals(method, _opts.VerifyMethod, StringComparison.OrdinalIgnoreCase))
                return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MethodMismatch);

            if (!string.Equals(path, _opts.VerifyPath, StringComparison.Ordinal))
                return VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.PathMismatch);

            return null;
        }

        private OperationResult<bool> TryValidateSignature(string token, SecurityKey key)
        {
            try
            {
                _tokenValidator.Validate(
                    token,
                    key,
                    _opts.Audience,
                    TimeSpan.FromSeconds(_opts.ClockSkewSeconds));
                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.SignatureInvalidOrExpired, ex.Message);
                return OperationResult<bool>.Failure(error);
            }
        }

        private readonly record struct OperationResult<T>
        {
            public bool IsSuccess { get; init; }
            public T Value { get; init; }
            public VerifyOutcome Error { get; init; }

            public static OperationResult<T> Success(T value) => new() { IsSuccess = true, Value = value, Error = null! };
            public static OperationResult<T> Failure(VerifyOutcome error) => new() { IsSuccess = false, Value = default!, Error = error };
        }
    }
}
