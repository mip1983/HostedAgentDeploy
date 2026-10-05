using Aspire.Hosting.Foundry;
using Azure.AI.Projects.Agents;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;

var builder = DistributedApplication.CreateBuilder(args);

var azureContainerApps = builder.AddAzureContainerAppEnvironment("agenttest-azure-mp123");

var apiService = builder.AddProject<Projects.HostedAgentDeploy_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithComputeEnvironment(azureContainerApps);

var secondApiService = builder.AddProject<Projects.HostedAgentDeploy_SecondApiService>("secondapiservice")
    .WithHttpHealthCheck("/health")
    .WithComputeEnvironment(azureContainerApps);

builder.AddProject<Projects.HostedAgentDeploy_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithComputeEnvironment(azureContainerApps);

// "Foundry User" (role id 53ca6127-db72-4b80-b1b0-d745d6d5456d) has no named member in
// CognitiveServicesBuiltInRole yet - the struct has an implicit string conversion, so the raw
// role GUID can be passed directly until the SDK adds a friendly name for it.
var foundryUserRole = (CognitiveServicesBuiltInRole)"53ca6127-db72-4b80-b1b0-d745d6d5456d";

var foundry = builder.AddFoundry("foundry");
var foundryProject = foundry.AddProject("project")
    .ConfigureInfrastructure(infra =>
    {
        CognitiveServicesAccount account = foundry.Resource.AddAsExistingResource(infra);
        var cogProject = infra.GetProvisionableResources().OfType<CognitiveServicesProject>().Single();

        RoleAssignment roleAssignment = new("foundry_project_foundry_user_role")
        {
            Name = BicepFunction.CreateGuid(account.Id, cogProject.Id, foundryUserRole.ToString()),
            Scope = new IdentifierExpression(account.BicepIdentifier),
            PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
            PrincipalId = cogProject.Identity.PrincipalId,
            RoleDefinitionId = BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", foundryUserRole.ToString())
        };
        infra.Add(roleAssignment);
    });

var modelDeployment = foundry.AddDeployment("aimodel", FoundryModel.OpenAI.Gpt54Nano)
    .WithProperties(config =>
    {
        config.SkuCapacity = 500;
        config.SkuName = "GlobalStandard";
    })
    .WithParentRelationship(foundry);

builder.AddProject<Projects.HostedAgentDeploy_Agent>("agent")
    .WithReference(foundryProject).WaitFor(foundryProject)
    .WithReference(modelDeployment).WaitFor(modelDeployment)
    .WithReference(apiService).WaitFor(apiService)  // Should only take first api as a deploy dependency
    .AsHostedAgent(foundryProject,
        config =>
        {
            config.ProtocolVersions.Add(new ProtocolVersionRecord(ProjectsAgentProtocol.Responses, "2.0.0"));
            config.ProtocolVersions.Add(new ProtocolVersionRecord(ProjectsAgentProtocol.Invocations, "1.0.0"));
        });

builder.Build().Run();
