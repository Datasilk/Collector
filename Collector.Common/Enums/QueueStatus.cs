namespace Collector.Data.Enums
{
    public enum QueueStatus
    {
        queued = 0,
        pulled = 1, //when pulled from the queue to download
        downloaded = 2
    }
}
