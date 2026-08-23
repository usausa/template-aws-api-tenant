namespace Template.Backend.Application;

// Reads the claims the API Gateway authorizer already verified. Stateless, so it stays a static
// helper rather than being pushed through the container.
//
// The tenant comes from the Cognito group membership: users belong to exactly one group named
// 'tenant-{id}', and the 'cognito:groups' claim is present in the access token (unlike custom
// attributes, which only appear in the ID token).
public static class Claims
{
    private const string TenantGroupPrefix = "tenant-";

    public static string Sub(APIGatewayHttpApiV2ProxyRequest request) => Read(request, "sub");

    public static string Username(APIGatewayHttpApiV2ProxyRequest request) => Read(request, "username");

    public static string Tenant(APIGatewayHttpApiV2ProxyRequest request)
    {
        // The claim value is the group list rendered as '[group1 group2]'.
        var groups = Read(request, "cognito:groups");
        foreach (var group in groups.Trim('[', ']').Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (group.StartsWith(TenantGroupPrefix, StringComparison.Ordinal))
            {
                return group[TenantGroupPrefix.Length..];
            }
        }

        return string.Empty;
    }

    private static string Read(APIGatewayHttpApiV2ProxyRequest request, string name)
    {
        var claims = request.RequestContext?.Authorizer?.Jwt?.Claims;
        return (claims is not null) && claims.TryGetValue(name, out var value) ? value : string.Empty;
    }
}
