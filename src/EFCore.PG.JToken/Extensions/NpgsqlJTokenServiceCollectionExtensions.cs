using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class NpgsqlJTokenServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkNpgsqlJObject(
    this IServiceCollection serviceCollection)
        {
            new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                        .TryAdd<IRelationalTypeMappingSourcePlugin, NpgsqlJTokenTypeMappingSourcePlugin>()
                        .TryAdd<IMethodCallTranslatorPlugin, NpgsqlJTokenMethodCallTranslatorPlugin>()
                        .TryAdd<IMemberTranslatorPlugin, NpgsqlJsonPocoMemberTranslatorPlugin>();

            return serviceCollection;
        }
    }
}
