using System.Text.Json.Serialization;

namespace PasskeyIntegrator.Api.Models.Mastercard;

// Mastercard Enroll Card Contracts (/cards)
public record MastercardEnrollCardRequest(
    [property: JsonPropertyName("fundingAccountInfo")] FundingAccountInfo FundingAccountInfo
);

public record FundingAccountInfo(
    [property: JsonPropertyName("pan")] string Pan
);

public record MastercardEnrollCardResponse(
    [property: JsonPropertyName("srcDigitalCardId")] string SrcDigitalCardId,
    [property: JsonPropertyName("status")] string Status
);

// Mastercard Orchestrator - Lookup
public record MastercardLookupRequest(
    [property: JsonPropertyName("srcDigitalCardId")] string SrcDigitalCardId
);

public record MastercardLookupResponse(
    [property: JsonPropertyName("accountHolderId")] string AccountHolderId
);

// Mastercard Orchestrator - Authenticators (Registration)
public record MastercardAuthenticatorsRequest(
    [property: JsonPropertyName("srcDigitalCardId")] string SrcDigitalCardId,
    [property: JsonPropertyName("fidoAttestationObject")] string? FidoAttestationObject = null,
    [property: JsonPropertyName("fidoClientDataJson")] string? FidoClientDataJson = null
);

public record MastercardAuthenticatorsResponse(
    [property: JsonPropertyName("fidoChallenge")] string? FidoChallenge,
    [property: JsonPropertyName("rpId")] string? RpId,
    [property: JsonPropertyName("userIdentifier")] string? UserIdentifier,
    [property: JsonPropertyName("registrationStatus")] string? RegistrationStatus,
    [property: JsonPropertyName("fidoCredentialId")] string? FidoCredentialId
);

// Mastercard Orchestrator - Authenticate (Payment Auth)
public record MastercardAuthenticateRequest(
    [property: JsonPropertyName("srcDigitalCardId")] string SrcDigitalCardId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("fidoAuthenticatorData")] string? FidoAuthenticatorData = null,
    [property: JsonPropertyName("fidoClientDataJson")] string? FidoClientDataJson = null,
    [property: JsonPropertyName("fidoSignature")] string? FidoSignature = null
);

public record MastercardAuthenticateResponse(
    [property: JsonPropertyName("fidoChallenge")] string? FidoChallenge,
    [property: JsonPropertyName("rpId")] string? RpId,
    [property: JsonPropertyName("authenticationStatus")] string? AuthenticationStatus,
    [property: JsonPropertyName("authorizationId")] string? AuthorizationId
);
