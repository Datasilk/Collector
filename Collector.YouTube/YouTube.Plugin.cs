using Collector.Common.Models;
using Collector.Common.Plugins;

namespace Collector.Plugins.YouTube
{
    /// <summary>
    /// Configuration for the Command Center Plugin system to enable support for finding content on YouTube
    /// </summary>
    public class Plugin : IPlugin
    {

        string IPlugin.Name => "YouTube";

        public string ID => "youtube";

        string IPlugin.Description => "Search YouTube for relavant content and build a list of videos to watch";

        decimal IPlugin.Version => 0.1M;

        string IPlugin.Author => "Mark Entingh";

        string IPlugin.AuthorUrl => "https://github.com/Datasilk/Collector";

        string IPlugin.PromptUseCase => "Collect a list of URL links from YouTube.com for videos related to the user's request";

        public Dictionary<string, string> SpeechPhrases => new Dictionary<string, string>()
        {
            {"write document", "Let's check YouTube for the best videos for you to watch" },
        };


        public Dictionary<string, string> Parameters => new Dictionary<string, string>()
        {
            {"search", "A description of the research being used to generate many YouTube search queries with" }
        };

        public Action<ExecutionPlan> OnExecute => (plan) => {
            // Dummy implementation that loads the current execution plan item
            var item = plan.CurrentTask;
            
            // Log or process the current execution plan item
            Console.WriteLine($"YouTube Plugin processing plan item " + (plan.CurrentTaskIndex));
        };
    }
}
