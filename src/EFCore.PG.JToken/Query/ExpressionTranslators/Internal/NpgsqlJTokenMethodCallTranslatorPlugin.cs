using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    public class NpgsqlJTokenMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {

        public NpgsqlJTokenMethodCallTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource, ISqlExpressionFactory sqlExpressionFactory)
        {
            Translators = new IMethodCallTranslator[]
            {
                new NpgsqlJsonMethodCallTranslator(typeMappingSource,(NpgsqlSqlExpressionFactory)sqlExpressionFactory)
            };
        }

        public virtual IEnumerable<IMethodCallTranslator> Translators { get; }
    }
    public class NpgsqlJsonMethodCallTranslator : IMethodCallTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly RelationalTypeMapping _stringTypeMapping;
        readonly RelationalTypeMapping _boolTypeMapping;
        readonly RelationalTypeMapping _jsonbTypeMapping;

        public NpgsqlJsonMethodCallTranslator(IRelationalTypeMappingSource typeMappingSource, NpgsqlSqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
            _stringTypeMapping = typeMappingSource.FindMapping(typeof(string));
            _boolTypeMapping = typeMappingSource.FindMapping(typeof(bool));
            _jsonbTypeMapping = typeMappingSource.FindMapping(typeof(Newtonsoft.Json.Linq.JToken));
        }

        public SqlExpression? Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (typeof(Newtonsoft.Json.Linq.JToken).IsAssignableFrom(method.DeclaringType) &&
            method.Name == "get_Item")
            {
                return (instance is ColumnExpression columnExpression
                    ? _sqlExpressionFactory.JsonTraversal(
                        columnExpression, returnsText: false, typeof(string), instance.TypeMapping)
                    : instance) is PostgresJsonTraversalExpression prevPathTraversal
                    ? prevPathTraversal.Append(_sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]))
                    : null;
            }
            if (arguments.FirstOrDefault() is PostgresJsonTraversalExpression traversal && method.Name == nameof(Newtonsoft.Json.Linq.Extensions.Value))
            {
                var traversalToText = new PostgresJsonTraversalExpression(
                    traversal.Expression,
                    traversal.Path,
                    returnsText: true,
                    typeof(string),
                    _stringTypeMapping);

                if (method.ReturnType == typeof(string))
                {
                    return traversalToText;
                }
                else
                {
                    return _sqlExpressionFactory.Convert(traversalToText, method.ReturnType, _typeMappingSource.FindMapping(method.ReturnType));
                }
            }

            return null;
        }
    }
}
