using System;
using System.Collections.Generic;
using System.Linq;

namespace AdditionalNetworking.Utils;

public static class Extensions
{
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

        return source
            .Select((x, i) => new { Value = x, Index = i })
            .GroupBy(x => x.Index / chunkSize)
            .Select(g => g.Select(x => x.Value));
    }
}