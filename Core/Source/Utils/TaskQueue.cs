using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasDev.YouTube
{
    /// <summary>
    ///  Represents a task factory to be queued in a TaskQueue
    /// </summary>
    public interface ITaskReference
    {
        Task ExecuteAsync();
    }

    public delegate void TaskReferenceExceptionHandler(ITaskReference sender, Exception exception);

    /// <summary>
    ///  This class can be used to queue and run asyncronous task
    /// </summary>
    public class TaskQueue
    {
        /// <summary>
        ///  This event is invoked when a task of the queue fails while it is being execuited
        /// </summary>
        public event TaskReferenceExceptionHandler TaskFailure;

        /// <summary>
        ///  Specifies how many tasks can run simultaneously while dequeuing the queue
        /// </summary>
        public readonly int ParallelismLevel;

        /// <summary>
        ///  If set to true, the dequeue process is stopped (and an exception is thrown) if a task fails while being executed
        /// </summary>
        public bool ThrowOnTaskFailure
        {
            get { return _throwOnTaskFailure || TaskFailure != null; }
            set { _throwOnTaskFailure = value; }
        }

        /// <summary>
        ///  Gets the number of tasks currently in the queue
        /// </summary>
        public int Count { get; private set; }

        private bool _throwOnTaskFailure;
        private readonly IList<ISet<ITaskReference>> _queue;
        private bool _isDequeuing;
        private int _currentTaskSetIndex;

        public TaskQueue(int parallelismLevel)
        {
            if (parallelismLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(parallelismLevel));
            ParallelismLevel = parallelismLevel;
            _queue = new List<ISet<ITaskReference>>();
        }

        public void Enqueue(ITaskReference taskCreator)
        {
            lock (_queue)
                EnqueueInternal(taskCreator);
        }

        private void EnqueueInternal(ITaskReference taskCreator)
        {
            EnsureIsNotDequeuing();
            if (_queue.Count == _currentTaskSetIndex)
                _queue.Add(new HashSet<ITaskReference>());

            var taskSet = _queue[_currentTaskSetIndex];
            if (taskSet.Count == ParallelismLevel)
            {
                _currentTaskSetIndex++;
                EnqueueInternal(taskCreator);
            }
            else
            {
                taskSet.Add(taskCreator);
                Count++;
            }
        }

        /// <summary>
        ///  Waits for all the task in the queue to be completed
        /// </summary>
        public async Task DequeueAsync()
        {
            lock (_queue)
            {
                EnsureIsNotDequeuing();
                _isDequeuing = true;
            }

            try
            {
                if (ThrowOnTaskFailure)
                    await DequeueTasksThrowingOnFailure();
                else
                    await DequeueTasksRaisingOnFailure();
            }
            finally
            {
                lock (_queue)
                {
                    _queue.Clear();
                    Count = 0;
                    _currentTaskSetIndex = 0;
                    _isDequeuing = false;
                }
            }
        }

        private async Task DequeueTasksThrowingOnFailure()
        {
            foreach (var taskSet in _queue)
                await Task.WhenAll(taskSet.Select(reference => reference.ExecuteAsync()));
        }

        private async Task DequeueTasksRaisingOnFailure()
        {
            foreach (var taskSet in _queue)
            {
                var tasks = taskSet.Select(async reference =>
                {
                    try
                    {
                        await reference.ExecuteAsync();
                    }
                    catch (Exception e)
                    {
                        TaskFailure?.Invoke(reference, e);
                    }
                });
                await Task.WhenAll(tasks);
            }
        }

        private void EnsureIsNotDequeuing()
        {
            if (_isDequeuing)
                throw new NotSupportedException("Dequeue already in progress");
        }
    }
}