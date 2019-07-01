using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Automation.Common.Helpers
{
    public class TaskQueueHelper
    {
        public static ITaskQueue<T> CreateQueueTask<T>(Action<T> processAction)
        {
            var taskQueue = new TaskQueue<T>();
            taskQueue.SetProcessAction(processAction);

            return taskQueue;
        }

        public static ITaskQueue<T> CreateQueueTask<T>(Action<T, CancellationToken> processAction, int timeout)
        {
            var taskQueue = new TaskQueue<T>();
            taskQueue.SetProcessTimeoutAction(processAction);
            taskQueue.SetTimeout(timeout);

            return taskQueue;
        }
    }

    public interface ITaskQueue<T> : IDisposable
    {
        void Start(int customerCount = 1, bool enableTimeout = false);
        void Stop();
        void Enqueue(T task);
        void SetTimeout(int seconds);
        void SetProcessAction(Action<T> action);
        int Size { get; }
        int TimeOut { get; }
        Action<T> ProcessAction { get; }
        Action<T, CancellationToken> ProcessActionWithCancellation { get; }
    }

    public class TaskQueue<T> : ITaskQueue<T>
    {
        private ILogger<TaskQueue> Logger { get; set; }
        private readonly BufferBlock<T> _queue = new BufferBlock<T>();
        private CancellationTokenSource _cancellationTokenSource;

        public int Size => _queue.Count;
        public int TimeOut { get; private set; }
        public Action<T> ProcessAction { get; private set; }
        public Action<T, CancellationToken> ProcessActionWithCancellation { get; private set; }

        public TaskQueue()
        {
            Logger = NullLogger<TaskQueue>.Instance;
        }

        public void Enqueue(T task)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                throw new InvalidOperationException("cannot enqueue into a stopped queue");
            _queue.Post(task);
        }

        public void SetTimeout(int seconds)
        {
            TimeOut = seconds;
        }

        public void SetProcessAction(Action<T> action)
        {
            ProcessAction = action;
        }

        public void SetProcessTimeoutAction(Action<T, CancellationToken> action)
        {
            ProcessActionWithCancellation = action;
        }

        public void Start(int customerCount = 1, bool enableTimeout = false)
        {
            if (_cancellationTokenSource != null)
                throw new InvalidOperationException("Already started");
            _cancellationTokenSource = new CancellationTokenSource();

            for (var i = 0; i < customerCount; i++)
            {
                Logger.LogInformation($"Start customer: {i}");
                Task.Run(async () =>
                {
                    while (await _queue.OutputAvailableAsync())
                    {
                        try
                        {
                            var task = await _queue.ReceiveAsync();
                            if (enableTimeout)
                                ProcessTimeoutAction(task);
                            else
                                ProcessAction(task);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex, LogLevel.Error);
                        }

                        if (!_cancellationTokenSource.Token.IsCancellationRequested) continue;
                        if (_queue.Count == 0)
                            _queue.Complete();
                    }
                });
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (_cancellationTokenSource == null) return;

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_queue.Count > 0)
                Task.WaitAny(_queue.Completion);
            _cancellationTokenSource.Dispose();
        }

        public void ProcessTimeoutAction(T task)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeOut);
                try
                {
                    ProcessActionWithCancellation(task, cts.Token);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, LogLevel.Error);
                }
            }
        }
    }
}