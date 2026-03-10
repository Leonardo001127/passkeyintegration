using PasskeyIntegrator.Api.Endpoints;
using PasskeyIntegrator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddHttpClient<VisaPasskeyClient>(client => 
{
    client.BaseAddress = new Uri("https://sandbox.api.visa.com");
    // As per Visa Docs: Adding mock Authorization Bearer to simulate OAuth2 MLE context
    client.DefaultRequestHeaders.Add("Authorization", "Bearer mock-visa-oauth2-token-for-mle");
    client.DefaultRequestHeaders.Add("X-PAY-TOKEN", "mock-x-pay-token"); 
});

builder.Services.AddHttpClient<MastercardPasskeyClient>(client => 
{
    client.BaseAddress = new Uri("https://sandbox.api.mastercard.com");
    // As per Mastercard Docs: Adding mock Authorization Bearer to simulate Identity Cloud context
    client.DefaultRequestHeaders.Add("Authorization", "Bearer mock-mastercard-identity-token");
});

builder.Services.AddSingleton<IBrandRouterService, BrandRouterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapPasskeyEndpoints();

app.Run();
