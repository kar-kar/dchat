var builder = DistributedApplication.CreateBuilder(args);

var rabbit = builder.AddRabbitMQ("rabbit");

builder.AddProject<Projects.DChat_Application_SSR_Server>("dchat-application-ssr-server")
    .WithReference(rabbit);

builder.AddProject<Projects.DChat_Application_ISR_Server>("dchat-application-isr-server")
    .WithReference(rabbit);

builder.AddProject<Projects.DChat_Application_CSR_Server>("dchat-application-csr-server")
    .WithReference(rabbit);

builder.Build().Run();
