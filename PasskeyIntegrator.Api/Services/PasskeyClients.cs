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
    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        var request = new VisaRegisterOptionsRequest(pan);
        var response = await httpClient.PostAsJsonAsync("/v1/fido/register/options", request);
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
        var request = new VisaAuthOptionsRequest(pan, amount);
        var response = await httpClient.PostAsJsonAsync("/v1/fido/authenticate/options", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<VisaAuthOptionsResponse>();

        // Visa doesnt always require a separate userId dynamically during auth, returning pan snippet as mock ID
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
    public async Task<RegisterChallengeResponse> GenerateRegisterChallengeAsync(string pan)
    {
        var request = new MastercardRegistrationInitRequest(pan);
        var response = await httpClient.PostAsJsonAsync("/id-cloud/v1/fido/registration/options", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MastercardRegistrationInitResponse>();
        
        return new RegisterChallengeResponse(data!.Challenge, data.RpId, data.UserIdentifier);
    }

    public async Task<RegisterVerifyResponse> VerifyRegistrationAsync(RegisterVerifyRequest request)
    {
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
