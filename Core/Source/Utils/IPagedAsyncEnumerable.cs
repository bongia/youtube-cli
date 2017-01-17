using System.Collections.Generic;

namespace MasDev.YouTube
{
    public interface IPagedAsyncEnumerable<out TItem> : IAsyncEnumerable<IReadOnlyList<TItem>>
    {
    }
}