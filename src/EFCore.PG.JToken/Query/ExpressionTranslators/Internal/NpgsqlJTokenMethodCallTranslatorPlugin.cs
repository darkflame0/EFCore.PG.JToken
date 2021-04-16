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
using Newtonsoft.Json.Linq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    public class NpgsqlJTokenMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {

        public NpgsqlJTokenMethodCallTranslatorPlugin(IRelationalTypeMappingSource typeMappingSource, ISqlExpressionFactory sqlExpressionFactory, NpgsqlJsonPocoTranslator jsonPocoTranslator)
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
        private static readonly MethodInfo _enumerableAnyWithoutPredicate = typeof(Enumerable).GetTypeInfo()
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Single(mi => mi.Name == nameof(Enumerable.Any) && mi.GetParameters().Length == 1);

        readonly IRelationalTypeMappingSource _typeMappingSource;
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly NpgsqlJsonPocoTranslator _jsonPocoTranslator;
        readonly RelationalTypeMapping _stringTypeMapping;

        public NpgsqlJsonMethodCallTranslator(IRelationalTypeMappingSource typeMappingSource, NpgsqlSqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
            _jsonPocoTranslator = new NpgsqlJsonPocoTranslator(_typeMappingSource, _sqlExpressionFactory);
            _stringTypeMapping = typeMappingSource.FindMapping(typeof(string));
        }

        public SqlExpression? Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {

            if (typeof(JToken).IsAssignableFrom(method.DeclaringType) &&
                method.Name == "get_Item" &&
                arguments.Count == 1)
            {
                return (instance is ColumnExpression columnExpression
                        ? _sqlExpressionFactory.JsonTraversal(
                            columnExpression, returnsText: false, typeof(string), instance.TypeMapping)
                        : instance) is PostgresJsonTraversalExpression prevPathTraversal
                        ? prevPathTraversal.Append(_sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]))
                        : null;
            }
            if (instance is PostgresJsonTraversalExpression traversal)
            {
                // Support for .Value<T>() and .Value<U, T>():
                if (instance == null &&
                    method.Name == nameof(Extensions.Value) &&
                    method.DeclaringType == typeof(Extensions) &&
                    method.IsGenericMethod &&
                    method.GetParameters().Length == 1 &&
                    arguments.Count == 1)
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

                // Support for Count()
                if (instance == null &&
                    method.Name == nameof(Enumerable.Count) &&
                    method.DeclaringType == typeof(Enumerable) &&
                    method.IsGenericMethod &&
                    method.GetParameters().Length == 1 &&
                    arguments.Count == 1)
                {
                    return _jsonPocoTranslator.TranslateArrayLength(traversal);
                }

                // Predicate-less Any - translate to a simple length check.
                if (method.IsClosedFormOf(_enumerableAnyWithoutPredicate) &&
                    arguments.Count == 1 &&
                    arguments[0].Type.TryGetElementType(out _) &&
                    arguments[0].TypeMapping is NpgsqlJsonTypeMapping)
                {
                    return _sqlExpressionFactory.GreaterThan(
                        _jsonPocoTranslator.TranslateArrayLength(arguments[0]),
                        _sqlExpressionFactory.Constant(0));
                }
            }
            return null;
        }
    }
}
