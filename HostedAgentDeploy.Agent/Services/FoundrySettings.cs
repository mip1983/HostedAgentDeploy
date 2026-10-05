using System.Data.Common;

namespace HostedAgentDeploy.Agent.Services;

public sealed record FoundrySettings(Uri ProjectUri, string DeploymentName)
{
    public static FoundrySettings FromConfiguration(IConfiguration configuration)
    {
        string projectEndpoint = ParseConnectionValue(
            configuration.GetConnectionString("project")
                ?? throw new InvalidOperationException($"Connection string 'project' is not set."),
            "Endpoint");

        string deploymentName = ParseConnectionValue(
            configuration.GetConnectionString("aimodel")
                ?? throw new InvalidOperationException($"Connection string 'aimodel' is not set."),
            "Deployment");

        if (!Uri.TryCreate(projectEndpoint, UriKind.Absolute, out Uri? projectUri))
            throw new InvalidOperationException($"'project' has an invalid Endpoint: '{projectEndpoint}'");

        return new(projectUri, deploymentName);
    }

    private static string ParseConnectionValue(string connectionString, string key)
    {
        DbConnectionStringBuilder csb = new() { ConnectionString = connectionString };
        var value = csb.TryGetValue(key, out object? raw) ? raw?.ToString() : null;

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Connection string is missing or has an empty '{key}' value.")
            : value!;
    }
}
