var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.PasskeyIntegrator_Api>("api");

var sdk = builder.AddNpmApp("sdk", "../sdk")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

var frontend = builder.AddNpmApp("frontend", "../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(sdk);

builder.Build().Run();
