using System.Collections.Concurrent;
using ExecutionContext = PandApache3.src.Module.ExecutionContext;


namespace PandApache3.src
{
    public class ResourceTaskScheduler : TaskScheduler
{
        private string _scheduleurName;
        private readonly BlockingCollection<Task> _taskQueue = new BlockingCollection<Task>();
        private readonly List<Thread> _workers;
        private bool _isDisposed = false;

        public ResourceTaskScheduler(string scheduleurName, int numberOfThreads)
        {
            _scheduleurName = scheduleurName;
            if (numberOfThreads <= 0)
            {
                throw new ArgumentException("Number of threads must be greater than zero.");
            }

            _workers = new List<Thread>(numberOfThreads);

            for (int i = 0; i < numberOfThreads; i++)
            {
                var worker = new Thread(Work) { IsBackground = true };
                _workers.Add(worker);
                worker.Start();
            }
        }

        private void Work()
        {
            foreach (var task in _taskQueue.GetConsumingEnumerable())
            {
                ExecutionContext.Current.Logger.LogInfo($"task {task.Id} in progress for {_scheduleurName}");
                TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            ExecutionContext.Current.Logger.LogInfo($"task {task.Id} queued for {_scheduleurName}");
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            _taskQueue.Add(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _taskQueue.ToList();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false; // Inline execution is not supported
        }

        public void Dispose()
        {
            _isDisposed = true;
            _taskQueue.CompleteAdding();
            foreach (var worker in _workers)
            {
                worker.Join();
            }
        }
    }
}
