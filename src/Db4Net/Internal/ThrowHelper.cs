using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Db4Net.Internal;

internal static class ThrowHelper
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
