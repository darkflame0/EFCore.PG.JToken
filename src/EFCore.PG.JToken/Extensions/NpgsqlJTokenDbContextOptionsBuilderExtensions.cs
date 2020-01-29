using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    public static class NpgsqlJTokenDbContextOptionsBuilderExtensions
    {
        public static NpgsqlDbContextOptionsBuilder UseJTokenTranslating(this NpgsqlDbContextOptionsBuilder optionsBuilder)
        {

            var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

            var extension = coreOptionsBuilder.Options.FindExtension<NpgsqlJTokenOptionsExtension>()
                            ?? new NpgsqlJTokenOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }
    }
}
