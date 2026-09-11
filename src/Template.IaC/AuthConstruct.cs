namespace Template.IaC;

using Amazon.CDK.AWS.Cognito;

// Authentication (User Pool) and tenant membership (user pool groups).
//
// A tenant is a user pool group named 'tenant-{id}'. Group membership travels in the
// 'cognito:groups' claim of the access token, which is what the HTTP API JWT authorizer
// verifies - so the handlers can trust the tenant without any extra lookup. Custom attributes
// are not used because they only appear in the ID token.
public sealed class AuthConstruct : Construct
{
    public AuthConstruct(Construct scope, string id, EnvironmentConfig config)
        : base(scope, id)
    {
        //--------------------------------------------------------------------------------
        // User Pool (authentication)
        //--------------------------------------------------------------------------------

        UserPool = new UserPool(this, "UserPool", new UserPoolProps
        {
            // Users are admin-issued only: tenant membership is a provisioning decision,
            // so self sign-up makes no sense here.
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Email = true },
            StandardAttributes = new StandardAttributes
            {
                Email = new StandardAttribute { Required = true, Mutable = true },
            },
            AccountRecovery = AccountRecovery.EMAIL_ONLY,
            FeaturePlan = FeaturePlan.ESSENTIALS,
            RemovalPolicy = config.Ephemeral ? RemovalPolicy.DESTROY : RemovalPolicy.RETAIN,
            DeletionProtection = !config.Ephemeral,
        });

        // API-only client: no hosted UI. Tokens are obtained with USER_PASSWORD_AUTH
        // (or SRP) via the CLI or a confidential caller; see scripts/seed-tenant-user.ps1.
        Client = UserPool.AddClient("Client", new UserPoolClientOptions
        {
            GenerateSecret = false,
            AuthFlows = new AuthFlow { UserPassword = true, UserSrp = true },
            PreventUserExistenceErrors = true,
            AccessTokenValidity = Duration.Minutes(60),
            IdTokenValidity = Duration.Minutes(60),
            RefreshTokenValidity = Duration.Days(30),
        });

        //--------------------------------------------------------------------------------
        // Tenant groups
        //--------------------------------------------------------------------------------

        foreach (var tenant in config.SampleTenants)
        {
            _ = new CfnUserPoolGroup(this, $"Tenant{tenant}", new CfnUserPoolGroupProps
            {
                UserPoolId = UserPool.UserPoolId,
                GroupName = $"tenant-{tenant}",
                Description = $"Members of tenant '{tenant}'",
            });
        }
    }

    public UserPool UserPool { get; }

    public UserPoolClient Client { get; }
}
