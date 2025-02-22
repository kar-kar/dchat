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
        .AddAzureSqlServer("dchatsqlserver")
        .AddDatabase("chatdb");
}

var rabbit = builder.AddRabbitMQ("rabbit")
    .WithManagementPlugin();

builder.AddProject<Projects.DChat_Application_SSR_Server>("dchat-application-ssr-server")
    .WithReference(chatdb)
    .WithReference(rabbit);

builder.Build().Run();
