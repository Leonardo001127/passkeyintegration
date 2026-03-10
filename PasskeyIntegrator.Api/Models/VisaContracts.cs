using System.Text.Json.Serialization;

namespace PasskeyIntegrator.Api.Models.Visa;

// OAuth2 Pushed Authorization Request (PAR)
public record VisaParRequest(
    [property: JsonPropertyName("response_type")] string ResponseType,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("redirect_uri")] string RedirectUri,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("code_challenge")] string CodeChallenge,
    [property: JsonPropertyName("code_challenge_method")] string CodeChallengeMethod
);

public record VisaParResponse(
    [property: JsonPropertyName("request_uri")] string RequestUri,
    [property: JsonPropertyName("expires_in")] int ExpiresIn
);

// Visa FIDO Registration Contracts
public record VisaRegisterOptionsRequest([property: JsonPropertyName("primaryAccountNumber")] string PrimaryAccountNumber);
public record VisaRegisterOptionsResponse([property: JsonPropertyName("fidoChallenge")] string FidoChallenge, [property: JsonPropertyName("relyingPartyId")] string RelyingPartyId, [property: JsonPropertyName("userAccountId")] string UserAccountId);

public record VisaRegisterVerifyRequest(
    [property: JsonPropertyName("primaryAccountNumber")] string PrimaryAccountNumber, 
    [property: JsonPropertyName("fidoChallenge")] string FidoChallenge, 
    [property: JsonPropertyName("fidoAttestationObject")] string FidoAttestationObject, 
    [property: JsonPropertyName("fidoClientDataJson")] string FidoClientDataJson);

public record VisaRegisterVerifyResponse([property: JsonPropertyName("status")] string Status, [property: JsonPropertyName("credentialId")] string CredentialId);

// Visa FIDO Authentication Contracts
public record VisaAuthOptionsRequest(
    [property: JsonPropertyName("primaryAccountNumber")] string PrimaryAccountNumber,
    [property: JsonPropertyName("transactionAmount")] decimal TransactionAmount);
public record VisaAuthOptionsResponse([property: JsonPropertyName("fidoChallenge")] string FidoChallenge, [property: JsonPropertyName("relyingPartyId")] string RelyingPartyId);

public record VisaAuthVerifyRequest(
    [property: JsonPropertyName("primaryAccountNumber")] string PrimaryAccountNumber, 
    [property: JsonPropertyName("fidoChallenge")] string FidoChallenge, 
    [property: JsonPropertyName("fidoAuthenticatorData")] string FidoAuthenticatorData, 
    [property: JsonPropertyName("fidoClientDataJson")] string FidoClientDataJson, 
    [property: JsonPropertyName("fidoSignature")] string FidoSignature,
    [property: JsonPropertyName("transactionAmount")] decimal TransactionAmount);

public record VisaAuthVerifyResponse([property: JsonPropertyName("status")] string Status, [property: JsonPropertyName("transactionId")] string TransactionId);
