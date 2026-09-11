namespace Template.Backend.Models;

using Amazon.DynamoDBv2.DataModel;

// Tenant isolation is expressed in the key schema: every item lives under its tenant's partition,
// and the handlers only ever query with the tenant from the verified token.
[DynamoDBTable("TenantItem")]
public sealed class ItemEntity
{
    [DynamoDBHashKey]
    public string TenantId { get; set; } = default!;

    [DynamoDBRangeKey]
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
}
