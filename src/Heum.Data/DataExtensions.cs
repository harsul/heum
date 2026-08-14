using Microsoft.Extensions.Hosting;

namespace Heum.Data;

public static class DataExtensions
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddNpgsqlDbContext<HeumDbContext>("heumdb");

        return builder;
    }
}
