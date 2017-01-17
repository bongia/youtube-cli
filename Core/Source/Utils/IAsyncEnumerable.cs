using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MasDev.YouTube
{
    /// <summary>
    ///  Represents an IEnumerable in which each item is yielded asynchronously
    /// </summary>
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetEnumerator();
    }

    public interface IAsyncEnumerator<out T>
    {
        T Current { get; }
        Task<bool> MoveNextAsync();
        void Reset();
    }


    public static class AsyncEnumerableExtensions
    {
        /// <summary>
        ///  Equivalent to IEnumerable.Select but based on IAsyncEnumerable
        /// </summary>
        public static IAsyncEnumerable<TElement> Select<TSource, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TElement> selector)
        {
            return new SelectAsyncEnumerable<TSource, TElement>(source, selector);
        }

        /// <summary>
        ///  Equivalent to IEnumerable.SelectMany but based on IAsyncEnumerable
        /// </summary>
        public static IAsyncEnumerable<TElement> SelectMany<TSource, TElement>(this IAsyncEnumerable<IEnumerable<TSource>> source, Func<TSource, TElement> selector)
        {
            return new SelectManyAsyncEnumerable<TSource, TElement>(source, selector);
        }

        /// <summary>
        ///  Equivalent to IEnumerable.Where but based on IAsyncEnumerable
        /// </summary>
        public static IAsyncEnumerable<TSource> Where<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return new WhereAsyncEnumerable<TSource>(source, predicate);
        }

        /// <summary>
        ///  Equivalent to IEnumerable.ToList but based on IAsyncEnumerable
        /// </summary>
        public static async Task<IList<TSource>> ToListAsync<TSource>(this IAsyncEnumerable<TSource> source)
        {
            var result = new List<TSource>();
            var enumerator = source.GetEnumerator();
            while (await enumerator.MoveNextAsync())
                result.Add(enumerator.Current);
            return result;
        }

        public static IPagedAsyncEnumerable<TItem> AsPaged<TItem>(this IAsyncEnumerable<IReadOnlyList<TItem>> source)
        {
            return new PagedAsyncEnumerable<TItem>(source);
        }

        public static async Task<TAggregation> AggregateAsync<TSource, TAggregation>(this IAsyncEnumerable<TSource> source, Func<TAggregation, TSource, TAggregation> aggregator, TAggregation initialValue = default(TAggregation))
        {
            var enumerator = source.GetEnumerator();
            while (await enumerator.MoveNextAsync())
                initialValue = aggregator(initialValue, enumerator.Current);
            return initialValue;
        }

        public static IAsyncEnumerable<TSource> Take<TSource>(this IAsyncEnumerable<TSource> source, int count)
        {
            return new TakeAsyncEnumerable<TSource>(source, count);
        }

        public static IAsyncEnumerable<TSource> Skip<TSource>(this IAsyncEnumerable<TSource> source, int count)
        {
            return new SkipAsyncEnumerable<TSource>(source, count);
        }

        public static async Task ForEach<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action)
        {
            var enumerator = source.GetEnumerator();
            while (await enumerator.MoveNextAsync())
                action(enumerator.Current);
        }

        public static IAsyncEnumerable<TSource> ToAsync<TSource>(this IEnumerable<TSource> source)
        {
            return new ComputedAsyncEnumerable<TSource>(source);
        }

        public static IAsyncEnumerable<TCollection> FoldLeft<TSource, TCollection>(this IAsyncEnumerable<TSource> source, TCollection targetElement)
            where TCollection : ICollection<TSource>
        {
            return new FoldLeftAsyncEnumerable<TSource, TCollection>(source, targetElement);
        }

        public static IAsyncEnumerable<TCollection> FoldLeft<TSource, TCollection>(this IAsyncEnumerable<TSource> source)
            where TCollection : class, ICollection<TSource>, new()
        {
            return new FoldLeftAsyncEnumerable<TSource, TCollection>(source, new TCollection());
        }

        class SelectAsyncEnumerable<TSource, TElement> : AsyncEnumerable<TElement>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private readonly Func<TSource, TElement> _selector;

            public SelectAsyncEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, TElement> selector)
            {
                _source = source.GetEnumerator();
                _selector = selector;
            }

            protected override async Task<TElement> MoveNextAsync(TElement previous, int iterationIndex)
            {
                if (!await _source.MoveNextAsync())
                    throw new IterationFinishedException();
                return _selector(_source.Current);
            }
        }

        class SelectManyAsyncEnumerable<TSource, TElement> : AsyncEnumerable<TElement>
        {
            private readonly IAsyncEnumerator<IEnumerable<TSource>> _source;
            private IEnumerator<TSource> _currentEnumerator;
            private readonly Func<TSource, TElement> _selector;

            public SelectManyAsyncEnumerable(IAsyncEnumerable<IEnumerable<TSource>> source, Func<TSource, TElement> selector)
            {
                _source = source.GetEnumerator();
                _selector = selector;
            }

            protected override async Task<TElement> MoveNextAsync(TElement previous, int iterationIndex)
            {
                if (_currentEnumerator == null && !await _source.MoveNextAsync())
                    throw new IterationFinishedException();

                _currentEnumerator = _currentEnumerator ?? _source.Current.GetEnumerator();
                if (_currentEnumerator.MoveNext())
                    return _selector(_currentEnumerator.Current);
                else
                {
                    _currentEnumerator = null;
                    return await MoveNextAsync(previous, iterationIndex);
                }
            }
        }

        class WhereAsyncEnumerable<TSource> : AsyncEnumerable<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private readonly Func<TSource, bool> _predicate;

            public WhereAsyncEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                _source = source.GetEnumerator();
                _predicate = predicate;
            }

            protected override async Task<TSource> MoveNextAsync(TSource previous, int iterationIndex)
            {
                if (!await _source.MoveNextAsync())
                    throw new IterationFinishedException();

                var isValid = _predicate(_source.Current);
                while (!isValid)
                {
                    if (!await _source.MoveNextAsync())
                        throw new IterationFinishedException();
                    isValid = _predicate(_source.Current);
                }

                return _source.Current;
            }
        }

        class PagedAsyncEnumerable<TItem> : AsyncEnumerable<IReadOnlyList<TItem>>, IPagedAsyncEnumerable<TItem>
        {
            private readonly IAsyncEnumerator<IReadOnlyList<TItem>> _source;
            public PagedAsyncEnumerable(IAsyncEnumerable<IReadOnlyList<TItem>> source)
            {
                _source = source.GetEnumerator();
            }

            protected override async Task<IReadOnlyList<TItem>> MoveNextAsync(IReadOnlyList<TItem> previous, int iterationIndex)
            {
                if (!await _source.MoveNextAsync())
                    throw new IterationFinishedException();
                return _source.Current;
            }
        }

        class TakeAsyncEnumerable<TSource> : AsyncEnumerable<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private readonly int _count;
            private int _currentCount;

            public TakeAsyncEnumerable(IAsyncEnumerable<TSource> source, int count)
            {
                _source = source.GetEnumerator();
                _count = count;
            }

            protected override async Task<TSource> MoveNextAsync(TSource previous, int iterationIndex)
            {
                if (!await _source.MoveNextAsync() || _currentCount == _count)
                    throw new IterationFinishedException();
                _currentCount++;
                return _source.Current;
            }
        }

        class SkipAsyncEnumerable<TSource> : AsyncEnumerable<TSource>
        {
            private readonly IAsyncEnumerator<TSource> _source;
            private readonly int _count;
            private int _currentCount;

            public SkipAsyncEnumerable(IAsyncEnumerable<TSource> source, int count)
            {
                _source = source.GetEnumerator();
                _count = count;
            }

            protected override async Task<TSource> MoveNextAsync(TSource previous, int iterationIndex)
            {
                while (_currentCount < _count)
                {
                    if (!await _source.MoveNextAsync())
                        throw new IterationFinishedException();
                    _currentCount++;
                }

                if (!await _source.MoveNextAsync())
                    throw new IterationFinishedException();

                return _source.Current;
            }
        }

        class ComputedAsyncEnumerable<T> : AsyncEnumerable<T>
        {
            private readonly IEnumerator<T> _source;

            public ComputedAsyncEnumerable(IEnumerable<T> source)
            {
                _source = source.GetEnumerator();
            }

            protected override Task<T> MoveNextAsync(T previous, int iterationIndex)
            {
                if (!_source.MoveNext())
                    throw new IterationFinishedException();
                return Task.FromResult(_source.Current);
            }
        }

        class FoldLeftAsyncEnumerable<TElement, TCollection> : AsyncEnumerable<TCollection>
            where TCollection : ICollection<TElement>
        {
            private readonly IAsyncEnumerator<TElement> _source;
            private readonly TCollection _targetElement;
            private bool _alreadyFolded;

            public FoldLeftAsyncEnumerable(IAsyncEnumerable<TElement> source, TCollection targetElement)
            {
                _source = source.GetEnumerator();
                _targetElement = targetElement;
            }

            protected override async Task<TCollection> MoveNextAsync(TCollection previous, int iterationIndex)
            {
                if (_alreadyFolded)
                    throw new IterationFinishedException();
                _alreadyFolded = true;

                while (await _source.MoveNextAsync())
                    _targetElement.Add(_source.Current);
                return _targetElement;
            }
        }
    }
}