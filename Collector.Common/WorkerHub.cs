namespace Collector.Common
{
    public abstract class WorkerHub
    {
        protected DateTime Started {get; set; }
        protected DateTime LastUpdated {get; set; }

        public virtual string Route { get; set; } = "";

        public virtual void Start() { }
        public virtual void Stop() { }

        /// <summary>
        /// Request to send details about worker progress
        /// </summary>
        public virtual void Progress() { }
    }
}