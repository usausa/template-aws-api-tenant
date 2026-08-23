namespace Template.IaC;

public static class Program
{
    public static void Main()
    {
        var app = new App();

        var envName = app.Node.TryGetContext("env") as string ?? "dev";
        var config = EnvironmentConfig.Load(app, envName);

        _ = new Infrastructure(app, $"template-aws-api-tenant-{envName}", config, new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = EnvironmentConfig.Region,
            },
            Description = "Multi tenant API template (HTTP API + Cognito JWT authorizer + tenant scoped DynamoDB)",
        });

        app.Synth();
    }
}
