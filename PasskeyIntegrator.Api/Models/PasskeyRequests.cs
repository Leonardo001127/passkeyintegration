namespace PasskeyIntegrator.Api.Models;

public record RegisterRequest(string Pan);
public record RegisterChallengeResponse(string Challenge, string RpId, string UserId);
public record RegisterVerifyRequest(string Pan, string Challenge, string ClientDataJson, string AttestationObject);
public record RegisterVerifyResponse(bool Success, string Message, string CredentialId);

public record AuthRequest(string Pan, decimal Amount);
public record AuthChallengeResponse(string Challenge, string RpId, string UserId);
public record AuthVerifyRequest(string Pan, string Challenge, string ClientDataJson, string AuthenticatorData, string Signature, decimal Amount);
public record AuthVerifyResponse(bool Success, string TransactionId, string Status);
