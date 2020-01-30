using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    public class NpgsqlJTokenMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
    {

        public NpgsqlJTokenMethodCallTranslatorPlugin(ISqlExpressionFactory sqlExpressionFactory)
        {
            Translators = new IMethodCallTranslator[]
            {
                new NpgsqlJsonMethodCallTranslator((NpgsqlSqlExpressionFactory)sqlExpressionFactory)
            };
        }

        public virtual IEnumerable<IMethodCallTranslator> Translators { get; }
    }
    public class NpgsqlJsonMethodCallTranslator : IMethodCallTranslator
    {

        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly RelationalTypeMapping _stringTypeMapping;
        readonly RelationalTypeMapping _boolTypeMapping;
        readonly RelationalTypeMapping _jsonbTypeMapping;

        public NpgsqlJsonMethodCallTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _stringTypeMapping = sqlExpressionFactory.FindMapping(typeof(string));
            _boolTypeMapping = sqlExpressionFactory.FindMapping(typeof(bool));
            _jsonbTypeMapping = sqlExpressionFactory.FindMapping(typeof(Newtonsoft.Json.Linq.JToken));
        }

        public SqlExpression? Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        {
            if ((method.DeclaringType == typeof(Newtonsoft.Json.Linq.JObject) ||
                method.DeclaringType == typeof(Newtonsoft.Json.Linq.JToken)) &&
                method.Name == "get_Item")
            {
                if (arguments[0].TypeMapping == null)
                {
                    if (arguments[0] is SqlConstantExpression constant)
                    {
                        arguments = new[] { _sqlExpressionFactory.ApplyDefaultTypeMapping(constant) };
                    }
                    else if (arguments[0] is SqlUnaryExpression unary)
                    {
                        arguments = new[] { unary.Operand };
                    }
                }
                // The first time we see a JSON traversal it's on a column - create a JsonTraversalExpression.
                // Traversals on top of that get appended into the same expression.
                return instance is ColumnExpression columnExpression
                ? _sqlExpressionFactory.JsonTraversal(columnExpression, arguments, false, typeof(string), instance.TypeMapping)
                : instance is JsonTraversalExpression prevPathTraversal
                    ? prevPathTraversal.Append(_sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]))
                    : null;
            }
            if (method.Name == nameof(Newtonsoft.Json.Linq.Extensions.Value) && arguments.FirstOrDefault() is JsonTraversalExpression traversal)
            {
                var traversalToText = new JsonTraversalExpression(
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
                    return _sqlExpressionFactory.Convert(traversalToText, method.ReturnType, _sqlExpressionFactory.FindMapping(method.ReturnType));
                }
            }

            return null;
        }
    }
}
