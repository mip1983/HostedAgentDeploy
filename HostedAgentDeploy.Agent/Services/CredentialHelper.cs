using Azure.Core;
using Azure.Identity;

namespace HostedAgentDeploy.Agent.Services;

public static class CredentialHelper
{
    public static TokenCredential GetCredential(IConfiguration configuration)
    {
        if (Environment.GetEnvironmentVariable(DefaultAzureCredential.DefaultEnvironmentVariableName) is not null)
        {
            return new DefaultAzureCredential(DefaultAzureCredential.DefaultEnvironmentVariableName);
        }

        string? managedIdentityClientId = configuration["AZURE_CLIENT_ID"];

        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            return new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
        }

        if (!string.IsNullOrEmpty(configuration["IDENTITY_ENDPOINT"]))
        {
            return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
        }

        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeManagedIdentityCredential = true
        });
    }
}
