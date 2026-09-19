namespace Template.Backend.Functions;

using Amazon.Lambda.Annotations;

using Smart.Mapper;

using Template.Backend.Services;

// Tenant scoped CRUD. Every handler resolves the tenant from the verified token first;
// the data access layer never sees a tenant the caller does not belong to.
public sealed partial class ItemFunction
{
    private const int DefaultLimit = 20;

    private const int MaxLimit = 100;

    private readonly ItemService itemService;

    public ItemFunction(ItemService itemService)
    {
        this.itemService = itemService;
    }

    [Mapper]
    private static partial ItemResponse ToResponse(ItemEntity entity);

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> List(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        var tenant = Claims.Tenant(request);
        if (tenant.Length == 0)
        {
            return Json.Forbidden("The user does not belong to any tenant group.");
        }

        // Paged with ?limit= and ?token= (the token of the previous page). One page per call.
        if (!TryGetLimit(request, out var limit))
        {
            return Json.BadRequest("The limit query parameter is invalid.");
        }

        var token = GetQuery(request, "token");
        var (list, nextToken) = await itemService.QueryListAsync(tenant, String.IsNullOrEmpty(token) ? null : token, limit);

        return Json.Ok(new ItemListResponse(list.Select(ToResponse).ToList(), nextToken));
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

        return entity is not null ? Json.Ok(ToResponse(entity)) : Json.NotFound();
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

    private static bool TryGetLimit(APIGatewayHttpApiV2ProxyRequest request, out int limit)
    {
        var value = GetQuery(request, "limit");
        if (String.IsNullOrEmpty(value))
        {
            limit = DefaultLimit;
            return true;
        }

        return Int32.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out limit) && limit is >= 1 and <= MaxLimit;
    }

    private static string? GetQuery(APIGatewayHttpApiV2ProxyRequest request, string name) =>
        (request.QueryStringParameters is not null) && request.QueryStringParameters.TryGetValue(name, out var value) ? value : null;

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
