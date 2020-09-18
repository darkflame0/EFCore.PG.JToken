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
            _stringTypeMapping = typeMappingSource.FindMapping(typeof(string));
        }

        public SqlExpression? Translate(SqlExpression instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance.TypeMapping is NpgsqlJsonTypeMapping mapping)
            {
                if ((member.DeclaringType.IsGenericCollection()
                    && member.Name == nameof(ICollection.Count)
                    || member.DeclaringType == typeof(Newtonsoft.Json.Linq.JArray) && member.Name == nameof(Newtonsoft.Json.Linq.JArray.Count)))
                {
                    return _sqlExpressionFactory.Function(
                       mapping.IsJsonb ? "jsonb_array_length" : "json_array_length",
                       new[] { instance },
                       nullable: true,
                       argumentsPropagateNullability: TrueArrays[1],
                       typeof(int));
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
            return null;
        }
    }
}
