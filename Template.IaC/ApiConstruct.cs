namespace Template.IaC;

using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AwsApigatewayv2Authorizers;
using Amazon.CDK.AwsApigatewayv2Integrations;

// Both the API and Lambda namespaces define HttpMethod.
using HttpMethod = Amazon.CDK.AWS.Apigatewayv2.HttpMethod;

// Authenticated API: HTTP API -> Cognito JWT authorizer -> Lambda.
//
// The authorizer is attached to every route, so an unauthenticated call is rejected with 401
// before any function is invoked. The handlers then derive the tenant from the verified
// 'cognito:groups' claim - no authorization logic beyond that lives in the functions.
//
// Routing lives here rather than in [HttpApi] annotations so that the whole infrastructure stays
// described in one place. The handler names below are produced by the Lambda Annotations source
// generator: {Assembly}::{Namespace}.{Class}_{Method}_Generated::{Method}.
public sealed class ApiConstruct : Construct
{
    // Published output of the Template.Backend project, produced by scripts/deploy-api.ps1. Every
    // function shares this one artifact and differs only by handler.
    private static readonly string Artifact =
        System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "publish-api");

    private readonly EnvironmentConfig config;

    private readonly ITable table;

    private readonly HttpUserPoolAuthorizer authorizer;

    public ApiConstruct(Construct scope, string id, EnvironmentConfig config, IUserPool userPool, IUserPoolClient userPoolClient, ITable table)
        : base(scope, id)
    {
        this.config = config;
        this.table = table;

        Api = new HttpApi(this, "Api", new HttpApiProps
        {
            Description = $"Multi tenant API ({config.EnvName})",
        });

        authorizer = new HttpUserPoolAuthorizer("Authorizer", userPool, new HttpUserPoolAuthorizerProps
        {
            UserPoolClients = [userPoolClient],

            // The API is called with an access token, which carries the 'cognito:groups' claim.
            IdentitySource = ["$request.header.Authorization"],
        });

        AddRoute("Tenant", HttpMethod.GET, "/tenant", "TenantFunction", "Handle");
        AddRoute("ItemList", HttpMethod.GET, "/items", "ItemFunction", "List");
        AddRoute("ItemGet", HttpMethod.GET, "/items/{id}", "ItemFunction", "Get");
        AddRoute("ItemCreate", HttpMethod.POST, "/items", "ItemFunction", "Create");
        AddRoute("ItemDelete", HttpMethod.DELETE, "/items/{id}", "ItemFunction", "Delete");
    }

    public HttpApi Api { get; }

    private void AddRoute(string name, HttpMethod method, string path, string functionClass, string functionMethod)
    {
        var function = new Function(this, $"{name}Function", new FunctionProps
        {
            Runtime = Runtime.DOTNET_10,
            Handler = $"Template.Backend::Template.Backend.Functions.{functionClass}_{functionMethod}_Generated::{functionMethod}",
            Code = Code.FromAsset(Artifact),
            MemorySize = 256,
            Timeout = Duration.Seconds(10),
            Environment = new Dictionary<string, string>
            {
                ["TABLE_NAME"] = table.TableName,
            },
            LogGroup = new LogGroup(this, $"{name}Logs", new LogGroupProps
            {
                Retention = config.Ephemeral ? RetentionDays.ONE_WEEK : RetentionDays.ONE_MONTH,
                RemovalPolicy = config.Ephemeral ? RemovalPolicy.DESTROY : RemovalPolicy.RETAIN,
            }),
            Description = $"{name} API ({config.EnvName})",
        });

        table.GrantReadWriteData(function);

        Api.AddRoutes(new AddRoutesOptions
        {
            Path = path,
            Methods = [method],
            Authorizer = authorizer,
            Integration = new HttpLambdaIntegration($"{name}Integration", function),
        });
    }
}
