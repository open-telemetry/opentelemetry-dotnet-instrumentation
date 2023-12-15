namespace Extensions;

public static class ListExtensions
{
    public static List<T> AddIf<T>(this List<T> list, T item, bool condition)
    {
        if (condition)
        {
            list.Add(item);
        }

        return list;
    }
}
