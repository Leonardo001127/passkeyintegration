using System.Net.Http.Json;
using System.Text.Json;
using PasskeyIntegrator.Api.Models;
using PasskeyIntegrator.Api.Models.Visa;
using PasskeyIntegrator.Api.Models.Mastercard;

namespace PasskeyIntegrator.Api.Services;

public interface IPasskeyClient
{
    Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan);
    Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request);
    Task<AuthChallengeResponse> GenerateAuthChallengeAsync(string pan, decimal amount);
    Task<AuthVerifyResponse> VerifyAuthenticationAsync(AuthVerifyRequest request);
}

public class VisaPasskeyClient(HttpClient httpClient) : IPasskeyClient
{
    private async Task<VisaParResponse> ExecuteParFlow(string pan, decimal? amount = null)
    {
        var authDetails = new VisaAuthorizationDetails(
            Type: amount.HasValue ? "fido_authentication" : "fido_registration",
            PrimaryAccountNumber: pan,
            TransactionAmount: amount
        );

        var parRequest = new VisaParRequest(
            ResponseType: "code",
            ClientId: "visa-poc-client",
            Scope: "passkeys",
            RedirectUri: "https://localhost:17076/api/passkeys/callback",
            State: Guid.NewGuid().ToString("N"),
            CodeChallenge: "example-pkce-challenge",
            CodeChallengeMethod: "S256",
            AuthorizationDetails: [authDetails]
        );

        // All operations flow through PAR endpoint to obtain server_auth_data
        var response = await httpClient.PostAsJsonAsync("/vpp/v1/passkeys/oauth2/authorization/request/pushed", parRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<VisaParResponse>())!;
    }

    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        var parData = await ExecuteParFlow(pan);
        
        // In a real scenario, server_auth_data contains the Base64 JSON of the FIDO challenge. 
        // We mock parsing the fido challenge out of the returned server_auth_data string from PAR.
        string mockChallenge = parData.ServerAuthData ?? "mock-visa-challenge-from-par";

        return new RegisterChallengeResponse(mockChallenge, "visa.com", "user-" + pan[^4..]);
    }

    public async Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request)
    {
        var visaRequest = new VisaVerifyRequest(
            RequestUri: request.Challenge, // Linking the previously established PAR request_uri
            FidoAttestationObject: request.AttestationObject, 
            FidoClientDataJson: request.ClientDataJson
        );

        // Submitting Verification back through the Visa Passkeys Verify endpoint / PAR completion
        var response = await httpClient.PostAsJsonAsync("/vpp/v1/passkeys/oauth2/authorization/request/pushed", visaRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaVerifyResponse>();

        return new RegisterVerifyResponse(data!.Status == "APPROVED", "Visa Passkey registered successfully.", data.CredentialId ?? "new-cred-id");
    }

    public async Task<AuthChallengeResponse> GenerateAuthChallengeAsync(string pan, decimal amount)
    {
        var parData = await ExecuteParFlow(pan, amount);

        string mockChallenge = parData.ServerAuthData ?? "mock-visa-auth-challenge-from-par";

        return new AuthChallengeResponse(mockChallenge, "visa.com", "user-" + pan[^4..]);
    }

    public async Task<AuthVerifyResponse> VerifyAuthenticationAsync(AuthVerifyRequest request)
    {
        var visaRequest = new VisaVerifyRequest(
            RequestUri: request.Challenge,
            FidoAuthenticatorData: request.AuthenticatorData, 
            FidoClientDataJson: request.ClientDataJson, 
            FidoSignature: request.Signature
        );
            
        var response = await httpClient.PostAsJsonAsync("/vpp/v1/passkeys/oauth2/authorization/request/pushed", visaRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaVerifyResponse>();
        
        return new AuthVerifyResponse(data!.Status == "APPROVED", data.TransactionId ?? "tx-12345", data.Status);
    }
}

public class MastercardPasskeyClient(HttpClient httpClient) : IPasskeyClient
{
    private async Task<string> EnrollCardAsync(string pan)
    {
        // 1. Enroll Card with Mastercard Checkout Solutions to get srcDigitalCardId
        var enrollRequest = new MastercardEnrollCardRequest(new FundingAccountInfo(pan));
        var enrollResponse = await httpClient.PostAsJsonAsync("/cards", enrollRequest);
        enrollResponse.EnsureSuccessStatusCode();

        var enrollData = await enrollResponse.Content.ReadFromJsonAsync<MastercardEnrollCardResponse>();
        return enrollData!.SrcDigitalCardId;
    }

    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        string digitalCardId = await EnrollCardAsync(pan);

        // 2. Lookup the account holder
        var lookupRequest = new MastercardLookupRequest(digitalCardId);
        var lookupResponse = await httpClient.PostAsJsonAsync("/digital/accountholder/authentications/lookup", lookupRequest);
        lookupResponse.EnsureSuccessStatusCode();

        // 3. Get Authenticators (Registration Options)
        var authRequest = new MastercardAuthenticatorsRequest(digitalCardId);
        var authResponse = await httpClient.PostAsJsonAsync("/authenticators", authRequest);
        authResponse.EnsureSuccessStatusCode();

        var data = await authResponse.Content.ReadFromJsonAsync<MastercardAuthenticatorsResponse>();
        
        return new RegisterChallengeResponse(data!.FidoChallenge ?? "mock-mc-challenge", data.RpId ?? "mastercard.com", data.UserIdentifier ?? "mc-user");
    }

    public async Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request)
    {
        // Verification completes the registration by submitting the attestation to /authenticators
        // Note: For PoC, bridging PAN logic to retrieve the digitalCardId, normally tracked via session state
        var mcRequest = new MastercardAuthenticatorsRequest(
            SrcDigitalCardId: "mock-digital-card-id-from-state", 
            FidoAttestationObject: request.AttestationObject, 
            FidoClientDataJson: request.ClientDataJson
        );
        
        var response = await httpClient.PostAsJsonAsync("/authenticators", mcRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardAuthenticatorsResponse>();

        return new RegisterVerifyResponse(data!.RegistrationStatus == "SUCCESS", "Mastercard Passkey registered successfully.", data.FidoCredentialId ?? "mc-cred-id");
    }

    public async Task<AuthChallengeResponse> GenerateAuthChallengeAsync(string pan, decimal amount)
    {
        // Notice authentication logic calls /authenticate to fetch options
        var mcRequest = new MastercardAuthenticateRequest(
            SrcDigitalCardId: "mock-digital-card-id-from-state", 
            Amount: amount
        );
        
        var response = await httpClient.PostAsJsonAsync("/authenticate", mcRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardAuthenticateResponse>();

        return new AuthChallengeResponse(data!.FidoChallenge ?? "mock-mc-auth-challenge", data.RpId ?? "mastercard.com", "mc-user-" + pan[^4..]);
    }

    public async Task<AuthVerifyResponse> VerifyAuthenticationAsync(AuthVerifyRequest request)
    {
        // Verification completes the payment by submitting the assertion to /authenticate
        var mcRequest = new MastercardAuthenticateRequest(
            SrcDigitalCardId: "mock-digital-card-id-from-state", 
            Amount: request.Amount,
            FidoAuthenticatorData: request.AuthenticatorData, 
            FidoClientDataJson: request.ClientDataJson, 
            FidoSignature: request.Signature
        );
            
        var response = await httpClient.PostAsJsonAsync("/authenticate", mcRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardAuthenticateResponse>();
        
        return new AuthVerifyResponse(data!.AuthenticationStatus == "SUCCESS", data.AuthorizationId ?? "mc-auth-id", data.AuthenticationStatus);
    }
}
