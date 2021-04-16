using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    internal static class TypeExtensions
    {
        internal static bool IsGenericCollection(this Type type)
        => type.IsGenericType
            && (type.GetGenericTypeDefinition() == typeof(ICollection<>)
            || type.GetInterfaces().Any(a => a.GetGenericTypeDefinition() == typeof(ICollection<>)));
        internal static bool TryGetElementType(this Type type, out Type? elementType)
        {
            elementType = type.IsArray
                ? type.GetElementType()
                : type.IsGenericCollection()
                    ? type.GetGenericArguments()[0]
                    : null;
            return elementType != null;
        }
    }
}
