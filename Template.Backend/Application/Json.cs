namespace Template.Backend.Application;

// Builds the API Gateway responses. Kept in one place so every function returns the same shape
// and headers.
public static class Json
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static APIGatewayHttpApiV2ProxyResponse Ok<T>(T payload) =>
        Build(200, JsonSerializer.Serialize(payload, SerializerOptions));

    public static APIGatewayHttpApiV2ProxyResponse Created<T>(T payload) =>
        Build(201, JsonSerializer.Serialize(payload, SerializerOptions));

    public static APIGatewayHttpApiV2ProxyResponse NoContent() =>
        new() { StatusCode = 204 };

    public static APIGatewayHttpApiV2ProxyResponse BadRequest(string message) =>
        Build(400, JsonSerializer.Serialize(new ErrorResponse(message), SerializerOptions));

    public static APIGatewayHttpApiV2ProxyResponse Forbidden(string message) =>
        Build(403, JsonSerializer.Serialize(new ErrorResponse(message), SerializerOptions));

    public static APIGatewayHttpApiV2ProxyResponse NotFound() =>
        new() { StatusCode = 404 };

    public static bool TryParse<T>(string? body, out T? value)
        where T : class
    {
        if (String.IsNullOrEmpty(body))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(body, SerializerOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static APIGatewayHttpApiV2ProxyResponse Build(int statusCode, string body) =>
        new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Content-Type"] = "application/json",
            },
            Body = body,
        };
}
