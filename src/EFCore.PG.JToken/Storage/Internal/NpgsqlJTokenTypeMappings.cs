using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal
{
    internal class JTokenTypeMapping : NpgsqlJsonTypeMapping
    {
        public JTokenTypeMapping([NotNull] string storeType, [NotNull] Type clrType)
            : base(storeType, clrType)
        {
        }

        protected JTokenTypeMapping(RelationalTypeMappingParameters parameters, NpgsqlDbType npgsqlDbType)
            : base(parameters, npgsqlDbType)
        {
        }


        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new JTokenTypeMapping(parameters, NpgsqlDbType);

        public override ValueConverter Converter => new JTokenConverter();

        private class JTokenConverter : ValueConverter<JToken, string?>
        {
            public JTokenConverter() : base(model => model.HasValues ? model.ToString(Formatting.None) : null, json => JToken.Parse(json!))
            {
            }
        }
    }
}
