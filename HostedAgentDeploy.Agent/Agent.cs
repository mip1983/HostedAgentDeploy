using Azure.AI.Projects;
using Azure.Core;
using HostedAgentDeploy.Agent.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Agents.AI.Hosting;

var port = Environment.GetEnvironmentVariable("DEFAULT_AD_PORT") ?? "8088";
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

TokenCredential credential = CredentialHelper.GetCredential(builder.Configuration);

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("URLS")))
{
    builder.WebHost.UseUrls($"http://+:{port}", $"https://+:{port}");
}

var foundrySettings = FoundrySettings.FromConfiguration(builder.Configuration);
AIProjectClient projectClient = new(foundrySettings.ProjectUri, credential);

AIAgent agent = projectClient.AsAIAgent(foundrySettings.DeploymentName, "You are a helpful assistant");

builder.AddAIAgent("agent1", (_, _) =>
{
    return agent;
});

builder.Services.AddFoundryResponses(agent);

var app = builder.Build();

app.MapFoundryResponses();

app.Run();
