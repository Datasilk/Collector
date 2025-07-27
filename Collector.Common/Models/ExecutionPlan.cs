using Collector.Common.Plugins;

namespace Collector.Common.Models
{
    public class ExecutionPlan
    {
        /// <summary>
        /// The initial text written by the user that started this request
        /// </summary>
        public string UserRequest { get; set; } = "";

        public string Answer { get; set; } = "";
        /// <summary>
        /// All tasks that must complete successfully in order to complete the user's request
        /// </summary>
        public List<ExecutionPlanTask> Tasks { get; set; } = new List<ExecutionPlanTask>();
        private int _currentTask { get; set; } = 0;

        /// <summary>
        /// The current task that is being executed
        /// </summary>
        public ExecutionPlanTask CurrentTask
        {
            get
            {
                return Tasks[_currentTask];
            }
        }

        /// <summary>
        /// The index of the current task that is being executed (starting from 0)
        /// </summary>
        public int CurrentTaskIndex
        {
            get {  return _currentTask; }
        }

        public bool PreviousTask()
        {
            if (_currentTask > 0)
            {
                _currentTask--;
                return true;
            }
            return false;
        }

        public bool NextTask()
        {
            if (_currentTask < Tasks.Count - 1)
            {
                _currentTask++;
                return true;
            }
            return false;
        }

        public void SetCurrentTask(int index)
        {
            _currentTask = index;
        }

        public void Restart()
        {
            _currentTask = 0;
        }

        public List<ExecutePlanResource> Resources { get; set; } = new List<ExecutePlanResource> { };
    }

    public class ExecutionPlanTask
    {
        /// <summary>
        /// A short summary of what task we must complete in order to move onto the next task (or complete the user's request)
        /// </summary>
        public string Action { get; set; } = "";
        /// <summary>
        /// A description of parameters (each of which is either required or optional), where parameter values must be populated 
        /// by the AI response before executing the task and using this plugin to complete the task
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// The ID of the plugin that is used to find the instance of the plugin loaded in memory
        /// </summary>
        public string PluginId { get; set; } = "";
        /// <summary>
        /// Reference for the plugin that needs to execute the planned task
        /// </summary>
        public IPlugin? Plugin { get; set; }
    }

    public class ExecutePlanResource
    {
        public string Name { get; set; } = "";
        public string Key { get; set; } = "";
        public string Data { get; set; } = ""; //typically JSON object or plain-text
        public ExecutePlanResourceRef Reference { get; set; } = new ExecutePlanResourceRef();
    }

    public class ExecutePlanResourceRef
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string Uri { get; set; } = "";
        public string Author { get; set; }
        public DateTime Published { get; set; }
    }
}
