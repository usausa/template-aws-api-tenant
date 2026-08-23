namespace Template.IaC;

// Single-stack layout (the class name avoids the 'Stack' suffix to satisfy CA1711).
//
// Data and Auth depend on nothing here; the API needs the table (function environment and
// grants) and the user pool client (authorizer), so it is created last.
public sealed class Infrastructure : Stack
{
    public Infrastructure(Construct scope, string id, EnvironmentConfig config, IStackProps props)
        : base(scope, id, props)
    {
        var data = new DataConstruct(this, "Data", config);
        var auth = new AuthConstruct(this, "Auth", config);

        var api = new ApiConstruct(this, "Api", config, auth.UserPool, auth.Client, data.Table);

        //--------------------------------------------------------------------------------
        // Outputs (consumed by scripts/seed-tenant-user.ps1)
        //--------------------------------------------------------------------------------

        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = auth.UserPool.UserPoolId });
        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = auth.Client.UserPoolClientId });
        _ = new CfnOutput(this, "TableName", new CfnOutputProps { Value = data.Table.TableName });
        _ = new CfnOutput(this, "ApiEndpoint", new CfnOutputProps { Value = api.Api.ApiEndpoint });
    }
}
