using System.Text.Json.Serialization;

namespace PasskeyIntegrator.Api.Models.Visa;

// Visa OAuth2 Pushed Authorization Request (PAR)
public record VisaParRequest(
    [property: JsonPropertyName("response_type")] string ResponseType,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("redirect_uri")] string RedirectUri,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("code_challenge")] string CodeChallenge,
    [property: JsonPropertyName("code_challenge_method")] string CodeChallengeMethod,
    // Typically contains the context for registration or authentication (like the PAN)
    [property: JsonPropertyName("authorization_details")] VisaAuthorizationDetails[]? AuthorizationDetails = null
);

public record VisaAuthorizationDetails(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("primaryAccountNumber")] string? PrimaryAccountNumber = null,
    [property: JsonPropertyName("transactionAmount")] decimal? TransactionAmount = null
);

public record VisaParResponse(
    [property: JsonPropertyName("request_uri")] string RequestUri,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    // Vital payload containing the FIDO challenge details dynamically returned from the PAR endpoint
    [property: JsonPropertyName("server_auth_data")] string? ServerAuthData
);

// We drop the distinct `/v1/fido/register/options` and `/v1/fido/authenticate/options` 
// requests and responses, as ALL option generation flows natively through the Visa FAR / PAR endpoint.

// Verification however typically still requires submitting the signed assertion/attestation back
public record VisaVerifyRequest(
    [property: JsonPropertyName("request_uri")] string RequestUri,
    [property: JsonPropertyName("fidoAttestationObject")] string? FidoAttestationObject = null,
    [property: JsonPropertyName("fidoAuthenticatorData")] string? FidoAuthenticatorData = null,
    [property: JsonPropertyName("fidoClientDataJson")] string? FidoClientDataJson = null,
    [property: JsonPropertyName("fidoSignature")] string? FidoSignature = null
);

public record VisaVerifyResponse(
    [property: JsonPropertyName("status")] string Status, 
    [property: JsonPropertyName("credentialId")] string? CredentialId,
    [property: JsonPropertyName("transactionId")] string? TransactionId
);
