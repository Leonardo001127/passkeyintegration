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
    // Add custom headers, certs, Auth here for Mastercard
});

builder.Services.AddHttpClient<MastercardPasskeyClient>(client => 
{
    client.BaseAddress = new Uri("https://sandbox.api.mastercard.com");
    // Add custom headers, certs, Auth here for Mastercard
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
