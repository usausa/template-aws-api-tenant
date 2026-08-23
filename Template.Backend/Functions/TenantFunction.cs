namespace Template.Backend.Functions;

using Amazon.Lambda.Annotations;

// GET /tenant - returns the tenant identity the API Gateway authorizer verified.
//
// Only [LambdaFunction] is used, not [HttpApi]: routing stays in the CDK stack so the whole
// infrastructure remains described in one place. The annotation is here for the generated
// wrapper and dependency injection, not for deployment.
public sealed class TenantFunction
{
    // The generated wrapper calls this on an instance, so it cannot be static.
#pragma warning disable CA1822
    [LambdaFunction]
    public APIGatewayHttpApiV2ProxyResponse Handle(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        context.Logger.LogInformation($"Tenant requested. tenant=[{tenant}], requestId=[{context.AwsRequestId}]");

        return Json.Ok(new TenantResponse(tenant, Claims.Sub(request), Claims.Username(request)));
    }
#pragma warning restore CA1822
}
