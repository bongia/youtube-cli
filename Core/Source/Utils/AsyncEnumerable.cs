using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasDev.YouTube
{
    /// <summary>
    ///  Represents an IEnumerable in which each item is yielded asynchronously
    /// </summary>
    public abstract class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IList<T> _cache = new List<T>();
        private readonly object _lock = new object();
        private int _lastDiscoveredIndex = -1;
        private int? _enumerationFinishIndex;
        private TaskCompletionSource<bool> _downloadCompletionSource;

        private async Task<bool> MoveNextAsync()
        {
            try
            {
                var nextIndex = _lastDiscoveredIndex + 1;
                if (nextIndex > _enumerationFinishIndex.GetValueOrDefault(int.MaxValue))
                    return false;

                if (_cache.Count > nextIndex)
                {
                    _lastDiscoveredIndex = nextIndex;
                    return true;
                }

                var hasMoreElements = true;
                try
                {
                    var current = _lastDiscoveredIndex != -1 && _lastDiscoveredIndex < _cache.Count ?
                        _cache[_lastDiscoveredIndex] :
                        default(T);

                    var next = await MoveNextAsync(current, _lastDiscoveredIndex);
                    _cache.Add(next);
                    _lastDiscoveredIndex = nextIndex;
                }
                catch (IterationFinishedException)
                {
                    _enumerationFinishIndex = _lastDiscoveredIndex;
                    hasMoreElements = false;
                }

                _downloadCompletionSource.SetResult(hasMoreElements);
                return hasMoreElements;
            }
            finally
            {
                _downloadCompletionSource = null;
            }
        }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new AsyncEnumerator(this);
        }

        /// <summary>
        ///  An implementation of this method should return the current element or throw an IterationFinishedException when no more elements can be retrieved
        /// </summary>
        protected abstract Task<T> MoveNextAsync(T previous, int iterationIndex);

        class AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly AsyncEnumerable<T> _source;
            private int _currentIndex;

            public T Current { get { return _currentIndex == -1 || !IsAlreadyComputed(_currentIndex) ? default(T) : _source._cache[_currentIndex]; } }

            public AsyncEnumerator(AsyncEnumerable<T> source)
            {
                _source = source;
                Reset();
            }

            public void Reset()
            {
                _currentIndex = -1;
            }

            public async Task<bool> MoveNextAsync()
            {
                var nextIndex = _currentIndex + 1;
                var hasMoreElements = await MoveToAsync(nextIndex);
                if (hasMoreElements)
                    _currentIndex = nextIndex;
                return hasMoreElements;
            }

            private Task<bool> MoveToAsync(int index)
            {
                lock (_source._lock)
                {
                    if (index > _source._enumerationFinishIndex.GetValueOrDefault(int.MaxValue))
                        return Task.FromResult(false);

                    if (IsAlreadyComputed(index))
                        return Task.FromResult(true);

                    if (_source._downloadCompletionSource != null)
                        return _source._downloadCompletionSource.Task;

                    _source._downloadCompletionSource = new TaskCompletionSource<bool>();
                }
                return _source.MoveNextAsync();
            }

            private bool IsAlreadyComputed(int index)
            {
                return _source._cache.Count > index;
            }
        }
    }

    public class IterationFinishedException : Exception { }

    public static class AsyncEnumerable
    {
        public static IAsyncEnumerable<TResult> FromResult<TResult>(TResult result)
        {
            return new List<TResult> { result }.ToAsync();
        }

        public static IAsyncEnumerable<TResult> Empty<TResult>()
        {
            return new List<TResult> { }.ToAsync();
        }
    }
}