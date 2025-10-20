# SpruceID - Decentralized Identity Verification System

A complete implementation of a self-sovereign identity (SSI) authentication system using Decentralized Identifiers (DIDs) and JSON Web Tokens (JWTs).

## Overview

This project demonstrates a **challenge-response authentication protocol** based on verifiable credentials and decentralized identities. It consists of two complementary applications that work together to enable secure, privacy-preserving authentication without relying on centralized identity providers.

## Architecture

```
┌─────────────┐                          ┌──────────────┐
│   Holder    │                          │  Verifier    │
│  (Client)   │                          │  (Server)    │
└─────────────┘                          └──────────────┘
       │                                         │
       │  1. Request Challenge (POST /challenge) │
       │────────────────────────────────────────>│
       │                                         │
       │  2. Return nonce + expiration + aud     │
       │<────────────────────────────────────────│
       │                                         │
       │  3. Create & Sign JWT with DID          │
       │     (includes nonce, holder_id,         │
       │      method, path)                      │
       │                                         │
       │  4. Submit JWT (POST /verify)           │
       │────────────────────────────────────────>│
       │                                         │
       │                 5. Validate JWT:        │
       │                    - Resolve DID        │
       │                    - Verify signature   │
       │                    - Check nonce        │
       │                    - Validate claims    │
       │                                         │
       │  6. Return verification result          │
       │<────────────────────────────────────────│
       │                                         │
```

---

## Project 1: Verifier

### What is the Verifier?

The **Verifier** is an ASP.NET Core Web API that acts as the authentication server. It issues cryptographic challenges and validates identity proofs from credential holders.

### Why do we need the Verifier?

- **Challenge Issuance**: Generates time-limited nonces to prevent replay attacks
- **Identity Verification**: Validates that the holder possesses the private key associated with their DID
- **Security Enforcement**: Ensures tokens are properly signed, not expired, and bound to the correct HTTP request
- **Decentralized Trust**: Resolves public keys from DIDs without requiring a centralized certificate authority

### Key Features

- **Nonce-based Challenge System**: Issues single-use, expiring nonces for replay protection
- **DID JWK Resolution**: Extracts public keys from `did:jwk` identifiers
- **JWT Signature Validation**: Verifies ECDSA P-256 signatures
- **Request Binding**: Ensures tokens are bound to specific HTTP methods and paths
- **In-Memory Nonce Store**: Tracks used nonces (thread-safe)

### Technology Stack

- **.NET 7.0** - ASP.NET Core Web API
- **Microsoft.IdentityModel.Tokens** - JWT validation
- **ECDSA P-256** - Elliptic curve cryptography

### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/challenge` | POST | Issues a new nonce challenge |
| `/verify` | POST | Verifies a signed JWT token |

### Configuration

Configuration is managed via `appsettings.json`:

```json
{
  "Verifier": {
    "Audience": "urn:example:verifier",
    "VerifyMethod": "POST",
    "VerifyPath": "/verify",
    "NonceTtlSeconds": 120,
    "ClockSkewSeconds": 120
  }
}
```

### How to Run the Verifier

#### Prerequisites
- .NET 7.0 SDK or later

#### Steps

1. **Navigate to the verifier directory**:
   ```bash
   cd verifier
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

4. **Access the API**:
   - HTTPS: `https://localhost:7262`
   - HTTP: `http://localhost:5262`
   - Swagger UI: `https://localhost:7262/swagger` (in development mode)

#### Verify it's running

Test the challenge endpoint:
```bash
curl -X POST https://localhost:7262/challenge -k
```

Expected response:
```json
{
  "nonce": "base64url-encoded-random-value",
  "expires_at": "2025-10-18T12:30:00Z",
  "aud": "urn:example:verifier"
}
```

---

## Project 2: Holder

### What is the Holder?

The **Holder** is a .NET console application that represents a credential holder (client). It requests challenges, creates signed JWTs with its DID, and submits them for verification.

### Why do we need the Holder?

- **Identity Proof**: Demonstrates possession of a private key associated with a DID
- **Challenge Response**: Solves cryptographic challenges to prove liveness
- **Credential Presentation**: Creates verifiable presentations using JWT format
- **Client-Side Cryptography**: Generates and manages ECDSA key pairs

### Key Features

- **ECDSA Key Generation**: Creates P-256 elliptic curve key pairs
- **DID JWK Creation**: Generates `did:jwk` identifiers from public keys
- **JWT Signing**: Signs tokens with ECDSA-SHA256
- **Challenge Retrieval**: Fetches nonces from verifier
- **Verification Submission**: Sends signed JWTs to verifier

### Technology Stack

- **.NET 7.0** - Console Application
- **Microsoft.IdentityModel.JsonWebTokens** - JWT creation
- **System.IdentityModel.Tokens.Jwt** - JWT signing
- **ECDSA P-256** - Elliptic curve cryptography

### Workflow

1. **Generate Key Pair**: Creates ECDSA P-256 keys and derives a `did:jwk` identifier
2. **Request Challenge**: Calls `POST /challenge` on the verifier
3. **Build JWT Claims**:
   - `iss`: Holder's DID
   - `sub`: Holder ID
   - `aud`: Verifier audience
   - `nonce`: Challenge nonce
   - `method`: HTTP method (POST)
   - `path`: HTTP path (/verify)
   - `iat`, `nbf`, `exp`: Timestamp claims
4. **Sign JWT**: Signs with private key, includes `kid` (DID) in header
5. **Submit for Verification**: Calls `POST /verify` with signed JWT
6. **Display Result**: Shows verification status

### Configuration

Configuration via environment variables (with defaults):

| Variable | Default | Description |
|----------|---------|-------------|
| `HOLDER_ID` | `did:example:holder-123-test` | Holder identifier |
| `VERIFIER_BASE` | `https://localhost:7262` | Verifier base URL |
| `CHALLENGE_URL` | `{VERIFIER_BASE}/challenge` | Challenge endpoint |
| `VERIFY_URL` | `{VERIFIER_BASE}/verify` | Verify endpoint |

### How to Run the Holder

#### Prerequisites
- .NET 7.0 SDK or later
- Running Verifier instance

#### Steps

1. **Ensure the Verifier is running** (see above)

2. **Navigate to the holder directory**:
   ```bash
   cd holder
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

#### Optional: Configure with Environment Variables

```bash
# Linux/macOS
export HOLDER_ID="did:example:my-custom-holder"
export VERIFIER_BASE="https://localhost:7262"
dotnet run

# Windows (PowerShell)
$env:HOLDER_ID="did:example:my-custom-holder"
$env:VERIFIER_BASE="https://localhost:7262"
dotnet run

# Windows (CMD)
set HOLDER_ID=did:example:my-custom-holder
set VERIFIER_BASE=https://localhost:7262
dotnet run
```

#### Expected Output

```
Holder Application Starting...

1. Generating ECDSA key pair and DID...
   DID: did:jwk:eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6IjF2...

2. Requesting challenge from verifier...
   Received nonce: Xy9K_abc123...
   Expires at: 2025-10-18T12:30:00Z

3. Creating and signing JWT...
   Token created with claims: iss, sub, aud, nonce, method, path

4. Submitting JWT to verifier...
   Verification Status: 200 OK
   Response: {"status":"valid","holder_id":"did:example:holder-123-test",...}

Verification successful!
```

---

## Running Both Projects Together

### Option 1: Two Terminals

**Terminal 1** (Verifier):
```bash
cd verifier
dotnet run
```

**Terminal 2** (Holder):
```bash
cd holder
dotnet run
```

---

## Technical Details

### DID JWK Format

A `did:jwk` identifier embeds a JSON Web Key in the DID itself:

```
did:jwk:<base64url(JWK-JSON)>
```

Example:
```
did:jwk:eyJrdHkiOiJFQyIsImNydiI6IlAtMjU2IiwieCI6IjF2ZmtpN...
```

When decoded, the JWK contains:
```json
{
  "kty": "EC",
  "crv": "P-256",
  "x": "1vfki7...",
  "y": "9hjTnb..."
}
```

### JWT Structure

**Header**:
```json
{
  "alg": "ES256",
  "typ": "JWT",
  "kid": "did:jwk:eyJrdHki..."
}
```

**Payload**:
```json
{
  "iss": "did:jwk:eyJrdHki...",
  "sub": "did:example:holder-123-test",
  "aud": "urn:example:verifier",
  "nonce": "Xy9K_abc123...",
  "method": "POST",
  "path": "/verify",
  "iat": 1697630400,
  "nbf": 1697630400,
  "exp": 1697634000
}
```

**Signature**: ECDSA P-256 signature of `base64url(header).base64url(payload)`

---

## Development

### Build Both Projects

```bash
# Build verifier
cd verifier
dotnet build

# Build holder
cd ../holder
dotnet build
```

### Clean Build Artifacts

```bash
# Verifier
cd verifier
dotnet clean

# Holder
cd ../holder
dotnet clean
```

---