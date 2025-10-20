# JWT Signing and Verification - Detailed Explanation

This document explains **exactly how** the Holder signs JWTs and how the Verifier validates them, using the actual source code.

---

## Table of Contents

1. [Overview](#overview)
2. [Part 1: Holder Creates and Signs JWT](#part-1-holder-creates-and-signs-jwt)
3. [Part 2: Verifier Validates JWT Signature](#part-2-verifier-validates-jwt-signature)
4. [Complete Flow Example](#complete-flow-example)
5. [Mathematical Foundation](#mathematical-foundation)

---

## Overview

The system uses **ECDSA (Elliptic Curve Digital Signature Algorithm)** with the **P-256 curve** to create and verify digital signatures.

### Key Concepts:

- **Private Key**: Secret key known only to the Holder (never shared)
- **Public Key**: Derived from the private key, embedded in the DID, shared with everyone
- **Digital Signature**: Mathematical proof that the Holder signed the data using their private key
- **Verification**: Anyone with the public key can verify the signature is valid

---

## Part 1: Holder Creates and Signs JWT

### Step 1: Generate ECDSA Key Pair

**File**: `holder/Security/EcdsaJwtSigner.cs` (Line 17)

```csharp
_ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
```

**What happens:**
1. Creates a new ECDSA key pair using the **NIST P-256 curve** (also called secp256r1)
2. Generates a **random private key** (256-bit secret number)
3. Mathematically derives the **public key** from the private key using elliptic curve math

**Result:**
- **Private Key**: `d` (secret scalar, never shared)
- **Public Key**: Point `Q = (x, y)` on the P-256 curve

### Step 2: Build DID from Public Key

**File**: `holder/Security/EcdsaJwtSigner.cs` (Lines 40-57)

```csharp
private string BuildDidJwkKid(ECDsa ecdsa)
{
    // Export ONLY the public key (not private!)
    var p = ecdsa.ExportParameters(includePrivateParameters: false);

    // Helper to Base64URL encode bytes
    static string B64Url(byte[] b) => Base64UrlEncoder.Encode(b);

    // Create JWK (JSON Web Key) with public key coordinates
    var jwkPublic = new
    {
        kty = "EC",           // Key Type: Elliptic Curve
        crv = "P-256",        // Curve: P-256
        x = B64Url(p.Q.X!),   // X coordinate of public key point
        y = B64Url(p.Q.Y!)    // Y coordinate of public key point
    };

    // Serialize to JSON
    var json = JsonSerializer.Serialize(jwkPublic);
    // Example: {"kty":"EC","crv":"P-256","x":"abc123...","y":"def456..."}

    // Create DID by base64url encoding the JSON
    var didJwk = "did:jwk:" + Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
    // Example: did:jwk:eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6ImFiYzEyMy4uLiIsInkiOiJkZWY0NTYuLi4ifQ

    return didJwk;
}
```

**What happens:**
1. Extracts the **public key coordinates** (x, y) from the ECDSA key
2. Creates a JSON object in **JWK format** containing the public key
3. **Base64URL encodes** the JSON
4. Prepends `did:jwk:` to create the DID identifier

**Result:**
The DID contains the public key embedded within it. Anyone can extract the public key by reversing this process.

### Step 3: Create JWT Claims

**File**: `holder/App/HolderApp.cs` (Lines 34-41)

```csharp
var claims = new List<Claim>
{
    new("sub", _holderDid),          // Subject: who is making the claim
    new("holder_id", _holderDid),    // Holder ID (alternative to sub)
    new("nonce", ch.Nonce),          // Challenge nonce from verifier
    new("method", "POST"),           // HTTP method binding
    new("path", "/verify")           // HTTP path binding
};
```

**What happens:**
Creates a list of claims (statements) to include in the JWT payload.

### Step 4: Sign the JWT

**File**: `holder/Security/EcdsaJwtSigner.cs` (Lines 23-38)

```csharp
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
        SigningCredentials = new SigningCredentials(
            new ECDsaSecurityKey(_ecdsa) { KeyId = _kidDidJwk },
            SecurityAlgorithms.EcdsaSha256)  // Algorithm: ES256 (ECDSA with SHA-256)
    };

    return handler.CreateEncodedJwt(descriptor);
}
```

**What happens:**

1. **Creates JWT Header**:
   ```json
   {
     "alg": "ES256",
     "typ": "JWT",
     "kid": "did:jwk:eyJrdHk..."
   }
   ```

2. **Creates JWT Payload**:
   ```json
   {
     "sub": "did:example:holder-123-test",
     "holder_id": "did:example:holder-123-test",
     "nonce": "Xy9K_abc123...",
     "method": "POST",
     "path": "/verify",
     "aud": "urn:example:verifier",
     "iat": 1697630400,
     "nbf": 1697630400,
     "exp": 1697634000
   }
   ```

3. **Encodes Header and Payload**:
   ```
   encodedHeader = base64url(JSON.stringify(header))
   encodedPayload = base64url(JSON.stringify(payload))
   ```

4. **Creates the Signing Input**:
   ```
   signingInput = encodedHeader + "." + encodedPayload
   ```
   Example: `eyJhbGc...XVCMiJ9.eyJzdWI...vcmlmaWVyIn0`

5. **Signs Using ECDSA**:
   ```
   hash = SHA256(signingInput)
   signature = ECDSA_Sign(privateKey, hash)
   encodedSignature = base64url(signature)
   ```

6. **Assembles the JWT**:
   ```
   jwt = encodedHeader + "." + encodedPayload + "." + encodedSignature
   ```
   Example: `eyJhbGc...XVCMiJ9.eyJzdWI...vcmlmaWVyIn0.MEUCIQCg...xjwIgf3w`

**Result:**
A complete JWT with three parts:
- **Header** (algorithm and key ID)
- **Payload** (claims)
- **Signature** (cryptographic proof)

---

## Part 2: Verifier Validates JWT Signature

### Step 1: Receive JWT

**File**: `verifier/Verification/VerificationService.cs` (Line 36)

```csharp
public async Task<VerifyOutcome> VerifyAsync(VerifyRequest req)
```

The verifier receives a JWT string like:
```
eyJhbGc...XVCMiJ9.eyJzdWI...vcmlmaWVyIn0.MEUCIQCg...xjwIgf3w
```

### Step 2: Read JWT Without Validation

**File**: `verifier/TokenValidator/JwtTokenValidator.cs` (Line 10)

```csharp
public JwtSecurityToken Read(string token) => _handler.ReadJwtToken(token);
```

**What happens:**
1. Splits the JWT into three parts: `header.payload.signature`
2. Base64URL decodes the header and payload
3. **Does NOT verify the signature yet**
4. Parses the JSON to create a `JwtSecurityToken` object

**Result:**
Access to header and payload data, including the `kid` (DID).

### Step 3: Extract DID from JWT Header

**File**: `verifier/Verification/VerificationService.cs` (Lines 96-115)

```csharp
private OperationResult<SecurityKey> TryResolveKey(JwtSecurityToken jwt)
{
    var kid = jwt.Header.Kid;  // Example: "did:jwk:eyJrdHk..."

    if (string.IsNullOrWhiteSpace(kid))
    {
        var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.MissingKid);
        return OperationResult<SecurityKey>.Failure(error);
    }

    try
    {
        var key = _keyResolver.ResolveFromDid(kid);  // Extract public key from DID
        return OperationResult<SecurityKey>.Success(key);
    }
    catch (Exception ex)
    {
        var error = VerifyOutcome.Bad(StatusCodes.Status400BadRequest, ErrorCodes.KeyResolutionFailed, ex.Message);
        return OperationResult<SecurityKey>.Failure(error);
    }
}
```

### Step 4: Resolve Public Key from DID

**File**: `verifier/JwkKeyResolver/DidJwkKeyResolver.cs` (Lines 10-43)

```csharp
public SecurityKey ResolveFromDid(string didUrl)
{
    // Example didUrl: "did:jwk:eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6ImFiYzEyMy4uLiIsInkiOiJkZWY0NTYuLi4ifQ"

    const string prefix = "did:jwk:";
    if (!didUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        throw new SecurityTokenInvalidSigningKeyException("Unsupported DID method (expected did:jwk)");

    // Extract the base64url part after "did:jwk:"
    var b64 = didUrl.Substring(prefix.Length);
    // Example: "eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6ImFiYzEyMy4uLiIsInkiOiJkZWY0NTYuLi4ifQ"

    // Decode to get the JWK JSON
    var jwkJson = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(b64));
    // Example: {"kty":"EC","crv":"P-256","x":"abc123...","y":"def456..."}

    // Parse the JSON
    using var doc = JsonDocument.Parse(jwkJson);
    var jwk = doc.RootElement;

    // Validate it's an EC key
    var kty = jwk.GetProperty("kty").GetString();
    if (kty != "EC") throw new SecurityTokenInvalidSigningKeyException("Only EC JWK supported here");

    // Validate it's P-256 curve
    var crv = jwk.GetProperty("crv").GetString();
    if (crv is not "P-256") throw new SecurityTokenInvalidSigningKeyException("Only P-256 supported here");

    // Extract the public key coordinates
    var x = jwk.GetProperty("x").GetString();
    var y = jwk.GetProperty("y").GetString();

    // Decode from base64url to bytes
    var xBytes = Base64UrlEncoder.DecodeBytes(x);
    var yBytes = Base64UrlEncoder.DecodeBytes(y);

    // Create EC parameters with the public key point (x, y)
    var ecParams = new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = xBytes, Y = yBytes }  // Public key point
    };

    // Create an ECDSA instance with ONLY the public key (no private key)
    var ecdsa = ECDsa.Create(ecParams);
    return new ECDsaSecurityKey(ecdsa);
}
```

**What happens:**
1. Extracts the base64url part after `did:jwk:`
2. Base64URL decodes to get the JWK JSON
3. Parses the JSON to extract `x` and `y` coordinates
4. Creates an ECDSA public key from the coordinates
5. Returns a `SecurityKey` object containing **only the public key**

**Result:**
The verifier now has the **exact same public key** that the Holder used, extracted from the DID.

### Step 5: Verify the Signature

**File**: `verifier/TokenValidator/JwtTokenValidator.cs` (Lines 12-27)

```csharp
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
        IssuerSigningKey = key  // The public key we extracted from the DID
    };

    _handler.ValidateToken(token, parameters, out _);
}
```

**What happens internally:**

1. **Reconstruct Signing Input**:
   ```
   signingInput = encodedHeader + "." + encodedPayload
   ```

2. **Hash the Signing Input**:
   ```
   hash = SHA256(signingInput)
   ```

3. **Decode the Signature**:
   ```
   signature = base64url_decode(encodedSignature)
   ```

4. **Verify Signature Using Public Key**:
   ```
   isValid = ECDSA_Verify(publicKey, hash, signature)
   ```

   This checks if:
   - The signature was created by the private key corresponding to this public key
   - The data (header + payload) has not been tampered with

5. **Additional Validations**:
   - **Audience**: Checks `aud` claim matches expected value
   - **Lifetime**: Checks `exp` (expiration) and `nbf` (not before)
   - **Clock Skew**: Allows some time tolerance for clock differences

**Result:**
If signature verification succeeds, we **mathematically prove**:
1. The JWT was signed by someone with the private key
2. The public key in the DID corresponds to that private key
3. The data has not been modified since signing

---

## Complete Flow Example

Let me walk through a complete example with actual data:

### 1. Holder Generates Key Pair

```
Private Key (d):
  0x1234abcd... (secret, never shared)

Public Key Point (Q):
  x = 0x9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08
  y = 0x8e94b66c37e3e32f95ac77d8f4e5f2e7b1c9e847d5c3a8b2f4e6d7c9a1b3f5e2
```

### 2. Holder Creates DID

```json
JWK JSON:
{
  "kty": "EC",
  "crv": "P-256",
  "x": "n4bQgYhMfWWaL-qgxVrQFaO_TxsrC4Is0V1sFbDwCgg",
  "y": "jpS2bDfj4y-VrHfY9OXy57HJ6EfVw6iy9ObXyaGz9eI"
}

Base64URL Encode:
  eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6Im40YlFnWWhNZldXYUwtcWd4VnJRRmFPX1R4c3JDNElzMFYxc0ZiRHdDZ2ciLCJ5IjoianBTMmJEZmo0eS1Wckhm...

DID:
  did:jwk:eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6Im40YlFnWWhNZldXYUwtcWd4VnJRRmFPX1R4c3JDNElzMFYxc0ZiRHdDZ2ciLCJ5IjoianBTMmJEZmo0eS1Wckhm...
```

### 3. Holder Creates JWT

**Header:**
```json
{
  "alg": "ES256",
  "typ": "JWT",
  "kid": "did:jwk:eyJrdHk..."
}
```

**Payload:**
```json
{
  "sub": "did:example:holder-123",
  "nonce": "abc123xyz",
  "aud": "urn:example:verifier",
  "iat": 1697630400,
  "exp": 1697634000
}
```

**Encoding:**
```
encodedHeader = "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImRpZDpqd2s6ZXlKcmRIayJ9"
encodedPayload = "eyJzdWIiOiJkaWQ6ZXhhbXBsZTpob2xkZXItMTIzIiwibm9uY2UiOiJhYmMxMjN4eXoiLCJhdWQiOiJ1cm46ZXhhbXBsZTp2ZXJpZmllciIsImlhdCI6MTY5NzYzMDQwMCwiZXhwIjoxNjk3NjM0MDAwfQ"

signingInput = encodedHeader + "." + encodedPayload
```

**Signing:**
```
hash = SHA256(signingInput)
  = 0x5d41402abc4b2a76b9719d911017c592

signature = ECDSA_Sign(privateKey, hash)
  = [r: 0x1234..., s: 0x5678...] (DER encoded)

encodedSignature = "MEUCIQCg...xjwIgf3w"
```

**Final JWT:**
```
eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImRpZDpqd2s6ZXlKcmRIayJ9.eyJzdWIiOiJkaWQ6ZXhhbXBsZTpob2xkZXItMTIzIiwibm9uY2UiOiJhYmMxMjN4eXoiLCJhdWQiOiJ1cm46ZXhhbXBsZTp2ZXJpZmllciIsImlhdCI6MTY5NzYzMDQwMCwiZXhwIjoxNjk3NjM0MDAwfQ.MEUCIQCg...xjwIgf3w
```

### 4. Verifier Validates

**Extract Public Key from DID:**
```
kid = "did:jwk:eyJrdHk..."
→ base64url_decode → JWK JSON
→ parse JSON → extract x and y
→ create ECDsa public key
```

**Verify Signature:**
```
signingInput = encodedHeader + "." + encodedPayload
hash = SHA256(signingInput)
isValid = ECDSA_Verify(publicKey, hash, signature)
```

**Result:**
```
✓ Signature is valid
✓ Audience matches
✓ Token not expired
→ Authentication successful!
```

---

## Mathematical Foundation

### Why Can't Someone Forge a Signature?

**ECDSA (Elliptic Curve Digital Signature Algorithm)** is based on hard mathematical problems:

1. **Elliptic Curve Discrete Logarithm Problem (ECDLP)**:
   - Given public key `Q = d × G` (where G is the generator point)
   - It's computationally infeasible to find the private key `d`
   - This is what makes the private key secure

2. **Signature Creation** (Holder's side):
   ```
   k = random number
   r = (k × G).x coordinate
   s = k⁻¹ × (hash + r × privateKey) mod n
   signature = (r, s)
   ```

3. **Signature Verification** (Verifier's side):
   ```
   u1 = hash × s⁻¹ mod n
   u2 = r × s⁻¹ mod n
   point = (u1 × G) + (u2 × publicKey)

   Signature is valid if: point.x == r
   ```

**Key Point**: You need the private key to create `s` correctly. Without it, you cannot produce a valid signature that will verify against the public key.

### Why is This Secure?

- **Private Key Never Shared**: Only in the Holder's memory
- **Public Key Derived**: Mathematically derived from private key, but irreversible
- **One-Way Function**: Easy to verify, impossible to forge without the private key
- **Tamper-Evident**: Any change to the payload changes the hash, invalidating the signature

---

## Summary

### Holder (Signing):
1. Generate ECDSA key pair (private + public)
2. Embed public key in DID (`did:jwk:...`)
3. Create JWT header and payload
4. Sign: `signature = ECDSA_Sign(privateKey, SHA256(header.payload))`
5. Send: `header.payload.signature`

### Verifier (Verification):
1. Receive JWT
2. Extract DID from `kid` in header
3. Decode DID to get public key
4. Verify: `ECDSA_Verify(publicKey, SHA256(header.payload), signature)`
5. Check additional claims (audience, expiration, nonce)
6. Accept or reject

**The beauty**: The Holder never shares the private key, yet the Verifier can cryptographically prove the Holder signed the JWT!
