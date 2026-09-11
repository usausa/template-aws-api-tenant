namespace Template.Backend;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using Amazon.Lambda.Annotations;

using Microsoft.Extensions.DependencyInjection;

using Template.Backend.Services;

// Service registration for the generated function wrappers. Runs once per Lambda execution
// environment, so singletons registered here survive across warm invocations.
[LambdaStartup]
public sealed class Startup
{
    // The generated wrapper calls this on an instance, so it cannot be static.
#pragma warning disable CA1822
    public void ConfigureServices(IServiceCollection services)
    {
        var tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? "TenantItem";

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
        services.AddSingleton<IDynamoDBContext>(static p => new DynamoDBContextBuilder()
            .WithDynamoDBClient(() => (AmazonDynamoDBClient)p.GetRequiredService<IAmazonDynamoDB>())
            .Build());

        services.AddSingleton(p => new ItemService(
            p.GetRequiredService<IDynamoDBContext>(),
            p.GetRequiredService<TimeProvider>(),
            tableName));
    }
#pragma warning restore CA1822
}
