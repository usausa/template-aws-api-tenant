namespace Template.IaC;

using Amazon.CDK.AWS.DynamoDB;

// Tenant scoped data table. The partition key is the tenant id, so isolation is expressed in
// the key schema itself: the handlers only ever query with the tenant taken from the verified
// token, and a single query can never span two tenants.
public sealed class DataConstruct : Construct
{
    public DataConstruct(Construct scope, string id, EnvironmentConfig config)
        : base(scope, id)
    {
        Table = new Table(this, "Table", new TableProps
        {
            TableName = $"TenantItem-{config.EnvName}",
            PartitionKey = new Attribute { Name = "TenantId", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "Id", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = config.Ephemeral ? RemovalPolicy.DESTROY : RemovalPolicy.RETAIN,
        });
    }

    public Table Table { get; }
}
