using GraphQL.Builders;
using GraphQL.Types.Relay.DataObjects;
using StarWars.Types;

namespace StarWars.Extensions;

public static class ResolveFieldContextExtensions
{
    public static Connection<TU> GetPagedResults<T, TU>(this IResolveConnectionContext<T> context, StarWarsData data, List<string> ids)
        where TU : StarWarsCharacter
    {
        List<string> idList;
        List<TU> list;
        string? cursor;
        string? endCursor;
        var pageSize = context.PageSize ?? 20;

        if (context.IsUnidirectional || context.After != null || context.Before == null)
        {
            if (context.After != null)
            {
                idList = ids
                    .SkipWhile(x => !Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(x)).Equals(context.After))
                    .Take(context.First ?? pageSize).ToList();
            }
            else
            {
                idList = ids
                    .Take(context.First ?? pageSize).ToList();
            }
        }
        else
        {
            if (context.Before != null)
            {
                idList = ids.Reverse<string>()
                    .SkipWhile(x => !Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(x)).Equals(context.Before))
                    .Take(context.Last ?? pageSize).ToList();
            }
            else
            {
                idList = ids.Reverse<string>()
                    .Take(context.Last ?? pageSize).ToList();
            }
        }

        list = data.GetCharactersAsync(idList).Result as List<TU> ?? throw new InvalidOperationException($"GetCharactersAsync method should return list of '{typeof(TU).Name}' items.");
        cursor = list.Count > 0 ? list.Last().Cursor : null;
        endCursor = ids.Count > 0 ? Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ids.Last())) : null;

        return new Connection<TU>
        {
            Edges = list.Select(x => new Edge<TU> { Cursor = x.Cursor, Node = x }).ToList(),
            TotalCount = list.Count,
            PageInfo = new PageInfo
            {
                EndCursor = endCursor,
                HasNextPage = endCursor != null && cursor != endCursor,
            }
        };
    }
}
