using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    internal static class MethodInfoExtensions
    {
        internal static bool IsClosedFormOf(
            this MethodInfo methodInfo, MethodInfo genericMethod)
            => methodInfo.IsGenericMethod
               && Equals(
                   methodInfo.GetGenericMethodDefinition(),
                   genericMethod);
    }
}
