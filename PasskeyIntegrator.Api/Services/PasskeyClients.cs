using System.Net.Http.Json;
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
    private async Task<string> ExecuteParFlow()
    {
        // 1. Execute Pushed Authorization Request (PAR) to get request_uri
        var parRequest = new VisaParRequest(
            ResponseType: "code",
            ClientId: "visa-poc-client",
            Scope: "passkeys",
            RedirectUri: "https://localhost:17076/api/passkeys/callback",
            State: Guid.NewGuid().ToString("N"),
            CodeChallenge: "example-pkce-challenge",
            CodeChallengeMethod: "S256"
        );

        var parResponse = await httpClient.PostAsJsonAsync("/vpp/v1/passkeys/oauth2/authorization/request/pushed", parRequest);
        parResponse.EnsureSuccessStatusCode();

        var parData = await parResponse.Content.ReadFromJsonAsync<VisaParResponse>();
        return parData!.RequestUri;
    }

    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        string requestUri = await ExecuteParFlow();
        // The requestUri is typically passed to the frontend to redirect the user to Visa's OAuth portal.
        // For this backend-to-backend PoC, we append it to the FIDO options request as contextual proof.
        
        var request = new VisaRegisterOptionsRequest(pan);
        var response = await httpClient.PostAsJsonAsync($"/v1/fido/register/options?request_uri={requestUri}", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaRegisterOptionsResponse>();
        
        return new RegisterChallengeResponse(data!.FidoChallenge, data.RelyingPartyId, data.UserAccountId);
    }

    public async Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request)
    {
        var visaRequest = new VisaRegisterVerifyRequest(request.Pan, request.Challenge, request.AttestationObject, request.ClientDataJson);
        var response = await httpClient.PostAsJsonAsync("/v1/fido/register/verify", visaRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaRegisterVerifyResponse>();

        return new RegisterVerifyResponse(data!.Status == "APPROVED", "Visa Passkey registered successfully.", data.CredentialId);
    }

    public async Task<AuthChallengeResponse> GenerateAuthChallengeAsync(string pan, decimal amount)
    {
        string requestUri = await ExecuteParFlow();

        var request = new VisaAuthOptionsRequest(pan, amount);
        var response = await httpClient.PostAsJsonAsync($"/v1/fido/authenticate/options?request_uri={requestUri}", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaAuthOptionsResponse>();

        return new AuthChallengeResponse(data!.FidoChallenge, data.RelyingPartyId, "user-" + pan[^4..]);
    }

    public async Task<AuthVerifyResponse> VerifyAuthenticationAsync(AuthVerifyRequest request)
    {
        var visaRequest = new VisaAuthVerifyRequest(
            request.Pan, request.Challenge, request.AuthenticatorData, request.ClientDataJson, request.Signature, request.Amount);
            
        var response = await httpClient.PostAsJsonAsync("/v1/fido/authenticate/verify", visaRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaAuthVerifyResponse>();
        
        return new AuthVerifyResponse(data!.Status == "APPROVED", data.TransactionId, data.Status);
    }
}

public class MastercardPasskeyClient(HttpClient httpClient) : IPasskeyClient
{
    private async Task<string> EnrollCardAsync(string pan)
    {
        // 1. Enroll Card with Mastercard Checkout Solutions before Passkey bindings
        var enrollRequest = new MastercardEnrollCardRequest(new FundingAccountInfo(pan));
        var enrollResponse = await httpClient.PostAsJsonAsync("/checkout/v1/cards/enroll", enrollRequest);
        enrollResponse.EnsureSuccessStatusCode();

        var enrollData = await enrollResponse.Content.ReadFromJsonAsync<MastercardEnrollCardResponse>();
        return enrollData!.EnrollmentId;
    }

    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        string enrollmentId = await EnrollCardAsync(pan);

        // 2. Map the generated SRC Enrollment ID to the FIDO init request
        var request = new MastercardRegistrationInitRequest(enrollmentId);
        var response = await httpClient.PostAsJsonAsync("/id-cloud/v1/fido/registration/options", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardRegistrationInitResponse>();
        
        return new RegisterChallengeResponse(data!.Challenge, data.RpId, data.UserIdentifier);
    }

    public async Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request)
    {
        // Notice we are assuming the PAN is identical to the enrollment ID in this mockup bridge for simplicity,
        // in a production SDK the EnrollmentId orchestrates state across endpoints.
        var mcRequest = new MastercardRegistrationCompleteRequest(request.Pan, request.Challenge, request.AttestationObject, request.ClientDataJson);
        var response = await httpClient.PostAsJsonAsync("/id-cloud/v1/fido/registration/result", mcRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardRegistrationCompleteResponse>();

        return new RegisterVerifyResponse(data!.RegistrationStatus == "SUCCESS", "Mastercard Passkey registered successfully.", data.FidoCredentialId);
    }

    public async Task<AuthChallengeResponse> GenerateAuthChallengeAsync(string pan, decimal amount)
    {
        var request = new MastercardAuthenticationInitRequest(pan, amount);
        var response = await httpClient.PostAsJsonAsync("/id-cloud/v1/fido/authentication/options", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardAuthenticationInitResponse>();

        return new AuthChallengeResponse(data!.Challenge, data.RpId, "mc-user-" + pan[^4..]);
    }

    public async Task<AuthVerifyResponse> VerifyAuthenticationAsync(AuthVerifyRequest request)
    {
        var mcRequest = new MastercardAuthenticationCompleteRequest(
            request.Pan, request.Challenge, request.AuthenticatorData, request.ClientDataJson, request.Signature, request.Amount);
            
        var response = await httpClient.PostAsJsonAsync("/id-cloud/v1/fido/authentication/result", mcRequest);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardAuthenticationCompleteResponse>();
        
        return new AuthVerifyResponse(data!.AuthenticationStatus == "SUCCESS", data.AuthorizationId, data.AuthenticationStatus);
    }
}
