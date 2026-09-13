namespace Template.Backend.Functions;

using Amazon.Lambda.Annotations;

using Template.Backend.Services;

// Tenant scoped CRUD. Every handler resolves the tenant from the verified token first;
// the data access layer never sees a tenant the caller does not belong to.
public sealed class ItemFunction
{
    private readonly ItemService itemService;

    public ItemFunction(ItemService itemService)
    {
        this.itemService = itemService;
    }

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> List(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        var list = await itemService.QueryListAsync(tenant);

        return Json.Ok(new ItemListResponse(list.Count, list.Select(ItemMapper.ToResponse).ToList()));
    }

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> Get(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        if (!TryGetId(request, out var id))
        {
            return Json.BadRequest("The id path parameter is missing.");
        }

        var entity = await itemService.QueryAsync(tenant, id);

        return entity is not null ? Json.Ok(entity.ToResponse()) : Json.NotFound();
    }

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> Create(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        if (!Json.TryParse<ItemCreateRequest>(request.Body, out var parameter) ||
            String.IsNullOrEmpty(parameter!.Name) ||
            (parameter.Name.Length > 50))
        {
            return Json.BadRequest("The request body is invalid.");
        }

        var entity = await itemService.CreateAsync(tenant, parameter.Name, parameter.Value);

        context.Logger.LogInformation($"Item created. tenant=[{tenant}], id=[{entity.Id}]");

        return Json.Created(new ItemCreateResponse(entity.Id));
    }

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> Delete(
        APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        if (!TryGetId(request, out var id))
        {
            return Json.BadRequest("The id path parameter is missing.");
        }

        var deleted = await itemService.DeleteAsync(tenant, id);
        if (deleted)
        {
            context.Logger.LogInformation($"Item deleted. tenant=[{tenant}], id=[{id}]");
        }

        return deleted ? Json.NoContent() : Json.NotFound();
    }

    private static bool TryGetId(APIGatewayHttpApiV2ProxyRequest request, out string id)
    {
        if ((request.PathParameters is not null) &&
            request.PathParameters.TryGetValue("id", out var value) &&
            !String.IsNullOrEmpty(value))
        {
            id = value;
            return true;
        }

        id = string.Empty;
        return false;
    }
}
