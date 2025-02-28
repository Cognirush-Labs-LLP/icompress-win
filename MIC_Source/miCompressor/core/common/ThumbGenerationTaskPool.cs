using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace miCompressor.core.common
{
    internal class ThumbGenerationTaskPool
    {
        public static ThumbGenerationTaskPool Instance { get; } = new();
        public static int MaxConcurrentTasks => 20;


        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentStack<(string Key, Func<Task> TaskFunc)> _taskStack = new();
        private readonly ConcurrentDictionary<string, Task> _runningTasks = new();
        private readonly int _maxConcurrentTasks;

        private ThumbGenerationTaskPool()
        {
            _maxConcurrentTasks = MaxConcurrentTasks;
            _semaphore = new SemaphoreSlim(MaxConcurrentTasks);
        }

        /// <summary>
        /// Enqueues a task if it is not already present (based on key).
        /// Tasks will execute in Last-In-First-Out (LIFO) order.
        /// </summary>
        /// <param name="key">Unique identifier for the task.</param>
        /// <param name="taskFunc">Function returning a Task.</param>
        public void EnqueueTask(string key, Func<Task> taskFunc)
        {
            if (!_runningTasks.ContainsKey(key))
            {
                _taskStack.Push((key, taskFunc));

                // Process tasks asynchronously in the background without blocking the caller
                Task.Run(ProcessQueue);
            }
        }

        private async Task ProcessQueue()
        {
            while (_taskStack.TryPop(out var taskEntry))
            {
                if (!_runningTasks.TryAdd(taskEntry.Key, Task.CompletedTask))
                    continue; // Skip if already running

                await _semaphore.WaitAsync();

                // Run task in parallel without blocking other executions
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await taskEntry.TaskFunc();
                    }
                    finally
                    {
                        _runningTasks.TryRemove(taskEntry.Key, out _);
                        _semaphore.Release();
                    }
                });
            }
        }
    }
}
