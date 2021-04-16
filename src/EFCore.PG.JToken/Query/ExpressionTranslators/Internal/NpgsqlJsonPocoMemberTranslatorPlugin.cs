using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json.Linq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    public class NpgsqlJsonPocoMemberTranslatorPlugin : IMemberTranslatorPlugin
    {
        public NpgsqlJsonPocoMemberTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource, ISqlExpressionFactory sqlExpressionFactory)
        {
            Translators = new IMemberTranslator[]
            {
                new NpgsqlJsonPocoMemberTranslator(typeMappingSource,(NpgsqlSqlExpressionFactory)sqlExpressionFactory)
            };
        }

        public virtual IEnumerable<IMemberTranslator> Translators { get; }
    }

    internal class NpgsqlJsonPocoMemberTranslator : IMemberTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        private readonly NpgsqlJsonPocoTranslator _jsonPocoTranslator;
        private readonly RelationalTypeMapping _stringTypeMapping;
        static readonly bool[][] TrueArrays =
{
            Array.Empty<bool>(),
            new[] { true },
            new[] { true, true }
        };

        public NpgsqlJsonPocoMemberTranslator(IRelationalTypeMappingSource typeMappingSource, NpgsqlSqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
            _jsonPocoTranslator = new NpgsqlJsonPocoTranslator(_typeMappingSource, _sqlExpressionFactory);
            _stringTypeMapping = typeMappingSource.FindMapping(typeof(string));
        }

        public SqlExpression? Translate(SqlExpression instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance.TypeMapping is NpgsqlJsonTypeMapping mapping)
            {
                if (instance?.Type.IsGenericCollection() == true &&
                    member.Name == nameof(List<object>.Count) &&
                    instance.TypeMapping is null)
                {
                    return _jsonPocoTranslator.TranslateArrayLength(instance);
                }

                if (member.DeclaringType.IsGenericType
                    && (typeof(IDictionary<,>).IsAssignableFrom(member.DeclaringType.GetGenericTypeDefinition())
                    || member.DeclaringType.GetInterfaces().Any(a => a.IsGenericType
                    && a.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                    )
                {

                    if (member.Name == nameof(IDictionary.Keys))
                    {
                        var type = (member as PropertyInfo)!.PropertyType.GetGenericArguments()[0];
                        var realReturnType = typeof(List<>).MakeGenericType(type);
                        var sub = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
                            mapping.IsJsonb ? "jsonb_each_text" : "json_each_text",
                            new[] { instance },
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[1],
                            typeof(string))
                            );

                        var binary = new PostgresUnknownBinaryExpression(_sqlExpressionFactory.Fragment("select key"), sub, "from", typeof(string), _stringTypeMapping);
                        var r = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
                            "ARRAY",
                            new SqlExpression[] { binary },
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[1],
                            typeof(string[])));
                        if (type != typeof(string))
                        {
                            r = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Convert(r, realReturnType));
                        }
                        return r;
                    }
                    if (member.Name == nameof(IDictionary.Values))
                    {
                        var type = (member as PropertyInfo)!.PropertyType.GetGenericArguments()[0];
                        var realReturnType = typeof(List<>).MakeGenericType(type);
                        var sub = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
                            mapping.IsJsonb ? "jsonb_each_text" : "json_each_text",
                            new[] { instance },
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[1],
                            typeof(string))
                            );

                        var binary = new PostgresUnknownBinaryExpression(_sqlExpressionFactory.Fragment("select value"), sub, "from", typeof(string), _stringTypeMapping);
                        var r = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
                            "ARRAY",
                            new SqlExpression[] { binary },
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[1],
                            realReturnType));
                        if (type != typeof(string))
                        {
                            r = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Convert(r, realReturnType));
                        }
                        return r;
                    }
                }
            }
            else if (instance.TypeMapping is NpgsqlArrayTypeMapping arrayMapping
                && member.DeclaringType.IsGenericCollection()
                && member.Name == nameof(ICollection.Count))
            {
                return _sqlExpressionFactory.Function(
                    "cardinality",
                    new[] { instance },
                    nullable: true,
                    argumentsPropagateNullability: TrueArrays[1],
                    typeof(int?));
            }

            if (!typeof(JToken).IsAssignableFrom(member.DeclaringType))
            {
                return null;
            }

            if (member.Name == nameof(JToken.Root) &&
                instance is ColumnExpression column &&
                column.TypeMapping is NpgsqlJsonTypeMapping)
            {
                // Simply get rid of the RootElement member access
                return column;
            }
            return null;
        }
    }
}
