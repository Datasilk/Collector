using Collector.Common.Models;
using System.Text.Json;

namespace Collector.Common
{
    public class PlanBuilder
    {
        //the original text written by the user which initialized this request
        private string _userInput { get; set; }
        //any context that might help with the user's request
        private string _context { get; set; }

        //the plan to build & execute
        public ExecutionPlan Plan = new ExecutionPlan();
        public LLMs.Models Model { get; set; } = LLMs.Models.Qwen;

        public PlanBuilder(string userInput, string context = "")
        {
            _userInput = userInput;
            _context = context;
        }

        public async Task Run()
        {
            //first, ask the AI what subjects are involved in the user's request/question
            var result = await LLMs.Prompt(@$"", @$"", @$"");

            //next, lets ask the AI to create an execution plan
            result = await LLMs.Prompt(@$"Either answer the user's question, or build a plan to accomplish the user's request.

# Rules
* If you can accomplish the user's request by simply answering their question, do it.
* You will need to build a plan if the user requires you to do something more advanced other than answering a question, such as doing research on the internet, accessing 3rd-party APIs, or executing command-line prompts, for example
* The plan will be used to complete the user's request in the best way possible
* The plan must include one or more steps in order to complete the user's request
* Each step will utilize a single tool to complete the step with
* Each tool has a single purpose, which is described below:

# Tools
[
{string.Join(",\n", PluginSystem.GetAll().Select(plugin =>
@$"  {{
        ""Name"": ""{plugin.Name}"",
        ""PluginId"": ""{plugin.ID}"",
        ""Parameters"": {{{(
            plugin.Parameters.Count > 0 ?
            string.Join("\n", plugin.Parameters.Select(param =>
            @$"""{param.Key}"": ""{param.Value}"",")) : "")}}},
        ""UseCase"":""{plugin.PromptUseCase.Replace("\"", "\\\"")}""
    }}"))}
]

# Output
* Generate a JSON array object as the output, and do not output anything before or after the JSON object. no exceptions!
* Only output the ""Answer"" property if you have an answer for the user. 
* Make sure the answer is short and sweet because we are using AI credits to generate the text-to-speech
* The answer property will be used to generate an audio file of the AI persona talking to the user, so make the answer
* Only output the ""Tasks"" property if you need to execute a set of tasks to complete the users request
* You can output both ""Answer"" and ""Tasks"" properties.
* The JSON array will be an array of task objects
* each task object must contain an ""Action"" property the describes the action to take
* Describe the action in a way so that it can be used in an AI prompt for further processing
* each task will need to specify which tool it will use by including the PluginId associated with the tool
* Based on the required parameters of the tool, please provide the correct values for each parameter that the tool needs
* Some tools don't have any parameters. If so, exclude the Parameters property from your task's output
* the parameters object will be deserialized into a Dictionary object in C#
* use the following template for generating the JSON array object:

{{
    ""Answer"": ""(optional) The answer that you want to say to the user using speech"",
    ""Tasks"": [
        {{
            ""Action"": ""Navigate to provided Uri and retreive contents of web page article"",
            ""PluginId"": ""web-browser"",
            ""Parameters"": {{""uri"": ""https://www.youtube.com/search?q=quantum+computers""}}
        }}
    ]
}}
" + (!string.IsNullOrEmpty(_context) ? "# Context\n" + _context : "")
            , "", "the user's request is as follows:\n\n" + _userInput, Model);

            try
            {
                //process the AI response
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(result);
                Console.ResetColor();
                Plan = JsonSerializer.Deserialize<ExecutionPlan>(result);
                if (Plan.Tasks == null)
                {
                    Plan.Tasks = new List<ExecutionPlanTask>();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("No tasks were returned by the AI");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    //execution plan ready!
                    foreach (var task in Plan.Tasks) {
                        task.Plugin = PluginSystem.GetAll().Where(a => a.ID == task.PluginId).FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Error parsing AI response: " + result);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        public void UpdateContext(string context) => _context = context;
    }
}
