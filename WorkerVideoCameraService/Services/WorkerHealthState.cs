namespace WorkerVideoCameraService.Services
{
    public class WorkerHealthState
    {
        private readonly object _lock = new object();

        public DateTimeOffset? LastCycleStartedAt { get; private set; }
        public DateTimeOffset? LastCaptureCompletedAt { get; private set; }
        public Exception? LastException { get; private set; }
        public int ActiveCaptureTasks { get; private set; }

        public void MarkCycleStarted(int activeCaptureTasks)
        {
            lock (_lock)
            {
                LastCycleStartedAt = DateTimeOffset.UtcNow;
                ActiveCaptureTasks = activeCaptureTasks;
            }
        }

        public void MarkCaptureCompleted()
        {
            lock (_lock)
            {
                LastCaptureCompletedAt = DateTimeOffset.UtcNow;
                LastException = null;
            }
        }

        public void MarkFailure(Exception exception)
        {
            lock (_lock)
            {
                LastException = exception;
            }
        }

        public void SetActiveCaptureTasks(int activeCaptureTasks)
        {
            lock (_lock)
            {
                ActiveCaptureTasks = activeCaptureTasks;
            }
        }
    }
}
