﻿using System;
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
                ? _sqlExpressionFactory.JsonTraversal(
                    columnExpression, returnsText: false, typeof(string), instance.TypeMapping)
                : instance is PostgresJsonTraversalExpression prevPathTraversal
                    ? prevPathTraversal.Append(_sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]))
                    : null;
            }
            if (method.Name == nameof(Newtonsoft.Json.Linq.Extensions.Value) && arguments.FirstOrDefault() is PostgresJsonTraversalExpression traversal)
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
