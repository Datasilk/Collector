using Collector.Common.Models;

namespace Collector.Common.Plugins
{
    public interface IPlugin
    {
        public string Name { get; }
        /// <summary>
        /// The ID used to identify the plugin when the execution plan is created or updated. 
        /// Please only use a lowercase alpha numeric string and use dashes instead of spaces. (e.g. "my-custom-plugin")
        /// </summary>
        public string ID { get; }
        public string Description { get; }
        public decimal Version { get; }
        public string Author { get; }
        public string AuthorUrl { get; }    
        /// <summary>
        /// Text that will be injected into the AI prompt to describe when and how this plugin should be utilized. 
        /// Please keep the use-case description brief, typically just one or two sentences long.
        /// </summary>
        public string PromptUseCase { get; }

        /// <summary>
        /// Describe the parameters that the plan execution item will need to provide.
        /// Please keep the description value brief, typically just one or two sentences long.
        /// (e.g. ""uri"": ""the value of a URL, preferrably secured using HTTPS protocol"")
        /// </summary>
        public Dictionary<string, string> Parameters { get; }

        /// <summary>
        /// A list of things that the persona will be able to say to the user
        /// </summary>
        public Dictionary<string, string> SpeechPhrases { get; }

        /// <summary>
        /// The function to call when the Command Center wants the plugin to process the task.
        /// You are required to update the ExecutionPlan object with resources and update the 
        /// CurrentTask status in order to complete the task.
        /// </summary>
        public Action<ExecutionPlan> OnExecute { get; }
    }
}
