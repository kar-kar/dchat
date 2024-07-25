using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> chatdb;
var connectionString = builder.Configuration.GetConnectionString("chatdb");

if (!string.IsNullOrEmpty(connectionString))
{
    //if connection string is provided, use it
    chatdb = builder.AddConnectionString("chatdb");
}
else
{
    //otherwise, create a new database
    chatdb = builder
        .AddSqlServer("dchatsqlserver")
        .PublishAsAzureSqlDatabase()
        .AddDatabase("chatdb");
}

var rabbit = builder.AddRabbitMQ("rabbit");

builder.AddProject<Projects.DChat_Application_SSR_Server>("dchat-ssr")
    .WithReference(chatdb)
    .WithReference(rabbit);

builder.AddProject<Projects.DChat_Application_ISR_Server>("dchat-isr")
    .WithReference(chatdb)
    .WithReference(rabbit);

builder.AddProject<Projects.DChat_Application_CSR_Server>("dchat-csr")
    .WithReference(chatdb)
    .WithReference(rabbit);

builder.Build().Run();
