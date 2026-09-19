namespace Template.Backend.Services;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

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

    // One page of the tenant's items. The token comes from the previous page (null for the first);
    // the returned token is null on the last page.
    public async Task<(List<ItemEntity> List, string? Token)> QueryListAsync(string tenant, string? paginationToken, int limit)
    {
        var table = context.GetTargetTable<ItemEntity>(new GetTargetTableConfig { OverrideTableName = tableName });
        var search = table.Query(new QueryOperationConfig
        {
            Filter = new QueryFilter(nameof(ItemEntity.TenantId), QueryOperator.Equal, tenant),
            Limit = limit,
            PaginationToken = paginationToken,
        });

        var documents = await search.GetNextSetAsync();
        var list = context.FromDocuments<ItemEntity>(documents).ToList();
        return (list, search.IsDone ? null : search.PaginationToken);
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

    // Deleting with ReturnValues avoids a read-before-delete race: an empty document means the item did not exist.
    public async Task<bool> DeleteAsync(string tenant, string id)
    {
        var table = context.GetTargetTable<ItemEntity>(new GetTargetTableConfig { OverrideTableName = tableName });
        var document = await table.DeleteItemAsync(tenant, id, new DeleteItemOperationConfig { ReturnValues = ReturnValues.AllOldAttributes });
        return document.Count > 0;
    }
}
