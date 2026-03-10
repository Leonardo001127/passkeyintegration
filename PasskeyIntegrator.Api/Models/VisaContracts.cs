using System.Text.Json.Serialization;

namespace PasskeyIntegrator.Api.Models.Visa;

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
