﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public NpgsqlJsonPocoMemberTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource , ISqlExpressionFactory sqlExpressionFactory)
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

        public NpgsqlJsonPocoMemberTranslator(IRelationalTypeMappingSource typeMappingSource ,NpgsqlSqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
            _stringTypeMapping = typeMappingSource.FindMapping(typeof(string));
        }

        public SqlExpression? Translate(SqlExpression instance, MemberInfo member, Type returnType)
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
                       typeof(int));
                }

                if (member.DeclaringType.IsGenericType 
                    && (member.DeclaringType.GetGenericTypeDefinition() == typeof(IDictionary<,>) 
                    || member.DeclaringType.GetInterfaces().Any(a => a.IsGenericType 
                    && a.GetGenericTypeDefinition() == typeof(IDictionary<,>))) 
                    && member.Name == nameof(IDictionary.Keys))
                {
                    var keyType = (member as PropertyInfo)!.PropertyType.GetGenericArguments()[0];
                    var sub = _sqlExpressionFactory.ApplyDefaultTypeMapping(_sqlExpressionFactory.Function(
                        mapping.IsJsonb ? "jsonb_object_keys" : "json_object_keys",
                        new[] { instance },
                        typeof(string))
                        );

                    var binary = new SqlCustomBinaryExpression(_sqlExpressionFactory.Fragment("select *"), sub, "from", typeof(string), _stringTypeMapping);
                    SqlExpression r = _sqlExpressionFactory.Function(
                        "ARRAY",
                        new SqlExpression[] { binary },
                        returnType);
                    if (keyType == typeof(int))
                    {
                        r = _sqlExpressionFactory.Convert(r, typeof(int[]), _typeMappingSource.FindMapping(typeof(int[])));
                    }
                    return r;
                }
            }
            else if (instance.TypeMapping is NpgsqlArrayTypeMapping arrayMapping
                && member.DeclaringType.IsGenericCollection()
                && member.Name == nameof(ICollection.Count))
            {
                return _sqlExpressionFactory.Function(
                    "cardinality",
                    new[] { instance },
                    typeof(int?));
            }
            return null;
        }
    }
}
