using Collector.Common.Models;

namespace Collector.Common.Plugins
{
    public class Scholar : IPlugin
    {
        public string Name => "Scholar";

        public string ID => "scholar";

        public string Description => "The scholoar is used to extract knowledge from resources and write documents";

        public decimal Version => 1.0M;

        public string Author => "Collector";

        public string AuthorUrl => "";

        public string PromptUseCase => @"Takes all the resources that have been collected in previous task executions within this plan and writes a well-formatted document about all the information collected, along with links to all references used";

        public Dictionary<string, string> SpeechPhrases => new Dictionary<string, string>()
        {
            {"write document", "Okay. Let me write a document for you based on all the research that I've done." }
        };

        public Dictionary<string, string> Parameters => new Dictionary<string, string>(); //no parameters neccessary

        public Action<ExecutionPlan> OnExecute => (plan) => {
            this.Speak("write document");
            var item = plan.CurrentTask;
            Console.WriteLine($"Scholar Plugin processing plan item " + plan.CurrentTaskIndex);
            var response = LLMs.Prompt("", "", "");

        };
    }
}
