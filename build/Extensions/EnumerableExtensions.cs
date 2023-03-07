using JetBrains.Annotations;

namespace Extensions;

internal static class EnumerableExtensions
{
    public static async Task ForEachAsync<T>(this IEnumerable<T> enumerable, [InstantHandle] Func<T, Task> action)
    {
        foreach (var item in enumerable)
        {
            await action(item);
        }            
    }
}
