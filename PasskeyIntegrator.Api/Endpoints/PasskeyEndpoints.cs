using Microsoft.AspNetCore.Mvc;
using PasskeyIntegrator.Api.Models;
using PasskeyIntegrator.Api.Services;

namespace PasskeyIntegrator.Api.Endpoints;

public static class PasskeyEndpoints
{
    public static void MapPasskeyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/passkeys").WithTags("Passkey Integration");

        group.MapPost("/register/options", async ([FromBody] RegisterRequest request, [FromServices] IBrandRouterService router) =>
        {
            try {
                var client = router.GetClientForPan(request.Pan);
                var response = await client.GenerateRegisterChallengeAsync(request.Pan);
                return Results.Ok(response);
            } catch (Exception ex) { return Results.BadRequest(new { Error = ex.Message }); }
        });

        group.MapPost("/register/verify", async ([FromBody] RegisterVerifyRequest request, [FromServices] IBrandRouterService router) =>
        {
            try {
                var client = router.GetClientForPan(request.Pan);
                var response = await client.VerifyRegistrationAsync(request);
                return Results.Ok(response);
            } catch (Exception ex) { return Results.BadRequest(new { Error = ex.Message }); }
        });

        group.MapPost("/auth/options", async ([FromBody] AuthRequest request, [FromServices] IBrandRouterService router) =>
        {
            try {
                var client = router.GetClientForPan(request.Pan);
                var response = await client.GenerateAuthChallengeAsync(request.Pan, request.Amount);
                return Results.Ok(response);
            } catch (Exception ex) { return Results.BadRequest(new { Error = ex.Message }); }
        });

        group.MapPost("/auth/verify", async ([FromBody] AuthVerifyRequest request, [FromServices] IBrandRouterService router) =>
        {
            try {
                var client = router.GetClientForPan(request.Pan);
                var response = await client.VerifyAuthenticationAsync(request);
                if (response.Success)
                    return Results.Ok(response);
                return Results.BadRequest(response);
            } catch (Exception ex) { return Results.BadRequest(new { Error = ex.Message }); }
        });
    }
}
