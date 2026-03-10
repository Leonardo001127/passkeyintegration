using System.Text.Json.Serialization;

namespace PasskeyIntegrator.Api.Models.Mastercard;

// Mastercard FIDO Registration Contracts
public record MastercardRegistrationInitRequest([property: JsonPropertyName("pan")] string Pan);
public record MastercardRegistrationInitResponse([property: JsonPropertyName("challenge")] string Challenge, [property: JsonPropertyName("rpId")] string RpId, [property: JsonPropertyName("userIdentifier")] string UserIdentifier);

public record MastercardRegistrationCompleteRequest(
    [property: JsonPropertyName("pan")] string Pan, 
    [property: JsonPropertyName("challenge")] string Challenge, 
    [property: JsonPropertyName("attestationObject")] string AttestationObject, 
    [property: JsonPropertyName("clientDataJson")] string ClientDataJson);

public record MastercardRegistrationCompleteResponse([property: JsonPropertyName("registrationStatus")] string RegistrationStatus, [property: JsonPropertyName("fidoCredentialId")] string FidoCredentialId);

// Mastercard FIDO Authentication Contracts
public record MastercardAuthenticationInitRequest(
    [property: JsonPropertyName("pan")] string Pan,
    [property: JsonPropertyName("amount")] decimal Amount);
public record MastercardAuthenticationInitResponse([property: JsonPropertyName("challenge")] string Challenge, [property: JsonPropertyName("rpId")] string RpId);

public record MastercardAuthenticationCompleteRequest(
    [property: JsonPropertyName("pan")] string Pan, 
    [property: JsonPropertyName("challenge")] string Challenge, 
    [property: JsonPropertyName("authenticatorData")] string AuthenticatorData, 
    [property: JsonPropertyName("clientDataJson")] string ClientDataJson, 
    [property: JsonPropertyName("signature")] string Signature,
    [property: JsonPropertyName("amount")] decimal Amount);

public record MastercardAuthenticationCompleteResponse([property: JsonPropertyName("authenticationStatus")] string AuthenticationStatus, [property: JsonPropertyName("authorizationId")] string AuthorizationId);
