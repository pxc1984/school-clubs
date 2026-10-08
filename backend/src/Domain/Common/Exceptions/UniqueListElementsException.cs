using System.Runtime.CompilerServices;

namespace SchoolClubs.Domain.Common.Exceptions;

public class UniqueListElementsException(string what)
    : ArgumentException($"{what} shouldn't have duplicate elements", what)
{
    public static void ThrowIfHasDuplicates<T, TKey>(
        IEnumerable<T>? values,
        Func<T, TKey> keySelector,
        [CallerArgumentExpression(nameof(values))]
        string paramName = "")
    {
        if (values == null)
            return;

        var seen = new HashSet<TKey>();
        foreach (var value in values)
        {
            if (!seen.Add(keySelector(value)))
                throw new UniqueListElementsException(paramName);
        }
    }

    public static void ThrowIfHasDuplicates<T>(
        IEnumerable<T>? values,
        [CallerArgumentExpression(nameof(values))]
        string paramName = "")
        => ThrowIfHasDuplicates(values, x => x, paramName);
}
