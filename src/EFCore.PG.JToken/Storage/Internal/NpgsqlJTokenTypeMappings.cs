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

        //public override Expression GenerateCodeLiteral(object value)
        //{
        //    var defaultJsonLoadSettings = new Lazy<Expression>(() => Expression.New(typeof(JsonLoadSettings)));
        //    var parseMethod = new Lazy<MethodInfo>(() => typeof(JToken).GetMethod(nameof(JToken.Parse), new[] { typeof(string), typeof(JsonLoadSettings) }));

        //    return value switch
        //    {
        //        JToken jToken => Expression.Call(parseMethod.Value, Expression.Constant(jToken.ToString(Formatting.None)), defaultJsonLoadSettings.Value),
        //        string s => Expression.Constant(s),
        //        _ => throw new NotSupportedException("Cannot generate code literals for JSON POCOs.")
        //    };
        //}
        private class JTokenConverter : ValueConverter<JToken, string?>
        {
            public JTokenConverter() : base(model => model.HasValues ? model.ToString(Formatting.None) : null, json => JToken.Parse(json!))
            {
            }
        }
    }
}
