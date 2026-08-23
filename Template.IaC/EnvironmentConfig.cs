namespace Template.IaC;

// Reads per-environment settings from the cdk.json context. Switch with -c env=dev|prod.
public sealed class EnvironmentConfig
{
    // Target region.
    public const string Region = "ap-northeast-1";

    private EnvironmentConfig(string envName, IReadOnlyList<string> sampleTenants)
    {
        EnvName = envName;
        SampleTenants = sampleTenants;
    }

    public string EnvName { get; }

    // Tenant groups created with the stack so the API can be exercised right after deployment.
    // Real tenants are provisioned by scripts/seed-tenant-user.ps1 or an admin system.
    public IReadOnlyList<string> SampleTenants { get; }

    // dev tears down cleanly on stack deletion; prod retains data and users.
    public bool Ephemeral => !String.Equals(EnvName, "prod", StringComparison.Ordinal);

    public static EnvironmentConfig Load(App app, string envName)
    {
        if (app.Node.TryGetContext(envName) is not IDictionary<string, object> context)
        {
            throw new InvalidOperationException($"cdk.json has no context for environment '{envName}'.");
        }

        var tenants = context.TryGetValue("sampleTenants", out var value) && value is object[] array
            ? array.OfType<string>().ToList()
            : [];

        return new EnvironmentConfig(envName, tenants);
    }
}
