using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal
{
    class NpgsqlJTokenOptionsExtension : IDbContextOptionsExtension
    {
        DbContextOptionsExtensionInfo _info;
        public virtual DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);


        public void ApplyServices(IServiceCollection services) => services.AddEntityFrameworkNpgsqlJObject();

        public virtual void Validate(IDbContextOptions options)
        {
            var internalServiceProvider = options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider;
            if (internalServiceProvider != null)
            {
                using (var scope = internalServiceProvider.CreateScope())
                {
                    if (scope.ServiceProvider.GetService<IEnumerable<IRelationalTypeMappingSourcePlugin>>()
                            ?.Any(s => s is NpgsqlJTokenTypeMappingSourcePlugin) != true)
                    {
                        throw new InvalidOperationException($"{nameof(NpgsqlJTokenDbContextOptionsBuilderExtensions.UseJTokenTranslating)} requires {nameof(NpgsqlJTokenServiceCollectionExtensions.AddEntityFrameworkNpgsqlJObject)} to be called on the internal service provider used.");
                    }
                }
            }
        }

        sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            new NpgsqlJTokenOptionsExtension Extension
                => (NpgsqlJTokenOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override long GetServiceProviderHashCode() => 0;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["Npgsql:" + nameof(NpgsqlJTokenDbContextOptionsBuilderExtensions.UseJTokenTranslating)] = "1";

            public override string LogFragment => "using JToken ";
        }
    }
}
