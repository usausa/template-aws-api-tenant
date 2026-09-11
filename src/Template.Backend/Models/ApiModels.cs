namespace Template.Backend.Models;

public sealed record ErrorResponse(string Message);

public sealed record TenantResponse(string Tenant, string Sub, string Username);

public sealed record ItemResponse(string Id, string Name, int Value, DateTime CreatedAt);

public sealed record ItemListResponse(int Count, IReadOnlyList<ItemResponse> Items);

public sealed record ItemCreateRequest(string Name, int Value);

public sealed record ItemCreateResponse(string Id);
