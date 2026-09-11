namespace Template.Backend.Services;

using Amazon.DynamoDBv2.DataModel;

// All operations take the tenant verified by the authorizer, so no query can cross a tenant
// boundary. The table name is provided per environment (TABLE_NAME) and overrides the attribute
// default on every call.
public sealed class ItemService
{
    private readonly IDynamoDBContext context;

    private readonly TimeProvider timeProvider;

    private readonly string tableName;

    public ItemService(IDynamoDBContext context, TimeProvider timeProvider, string tableName)
    {
        this.context = context;
        this.timeProvider = timeProvider;
        this.tableName = tableName;
    }

    public async Task<List<ItemEntity>> QueryListAsync(string tenant)
    {
        var search = context.QueryAsync<ItemEntity>(tenant, new QueryConfig { OverrideTableName = tableName });
        return await search.GetRemainingAsync();
    }

    public Task<ItemEntity?> QueryAsync(string tenant, string id) =>
        context.LoadAsync<ItemEntity?>(tenant, id, new LoadConfig { OverrideTableName = tableName });

    public async Task<ItemEntity> CreateAsync(string tenant, string name, int value)
    {
        var entity = new ItemEntity
        {
            TenantId = tenant,
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Value = value,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        await context.SaveAsync(entity, new SaveConfig { OverrideTableName = tableName });

        return entity;
    }

    public async Task<bool> DeleteAsync(string tenant, string id)
    {
        var entity = await QueryAsync(tenant, id);
        if (entity is null)
        {
            return false;
        }

        await context.DeleteAsync<ItemEntity>(tenant, id, new DeleteConfig { OverrideTableName = tableName });

        return true;
    }
}
