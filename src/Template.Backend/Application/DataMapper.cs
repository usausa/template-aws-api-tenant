namespace Template.Backend.Application;

using Smart.Mapper;

internal static partial class DataMapper
{
    [Mapper]
    public static partial ItemResponse ToResponse(this ItemEntity entity);
}
