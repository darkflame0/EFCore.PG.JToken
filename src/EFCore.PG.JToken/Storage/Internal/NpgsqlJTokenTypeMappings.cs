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
    internal class JTokenTypeMapping : NpgsqlTypeMapping
    {
        public JTokenTypeMapping([NotNull] string storeType, [NotNull] Type clrType)
            : base(storeType, clrType, storeType == "jsonb" ? NpgsqlDbType.Jsonb : NpgsqlDbType.Json)
        {
            if (storeType != "json" && storeType != "jsonb")
                throw new ArgumentException($"{nameof(storeType)} must be 'json' or 'jsonb'", nameof(storeType));
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
