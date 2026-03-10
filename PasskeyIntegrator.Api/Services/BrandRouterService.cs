using System.Text.RegularExpressions;

namespace PasskeyIntegrator.Api.Services;

public interface IBrandRouterService
{
    IPasskeyClient GetClientForPan(string pan);
}

public class BrandRouterService(VisaPasskeyClient visaClient, MastercardPasskeyClient mastercardClient) : IBrandRouterService
{
    public IPasskeyClient GetClientForPan(string pan)
    {
        pan = Regex.Replace(pan, @"[^\d]", ""); // Remove non-numeric characters

        if (pan.StartsWith("4"))
            return visaClient;
        
        if (pan.StartsWith("5"))
            return mastercardClient;

        // Fallback or throw error for unsupported brands
        throw new NotSupportedException($"Card network not supported for PAN starting with {pan.Substring(0, Math.Min(4, pan.Length))}");
    }
}
