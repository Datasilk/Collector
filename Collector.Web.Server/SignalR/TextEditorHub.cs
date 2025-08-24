using Microsoft.AspNetCore.SignalR;
using Collector.Common;

namespace Collector.Web.Server.SignalR
{
    public class TextEditorHub : Hub
    {
        private readonly ILogger<TextEditorHub> _logger;

        public TextEditorHub(ILogger<TextEditorHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates content based on the provided prompt using the LLMs service
        /// </summary>
        /// <param name="prompt">User's prompt for content generation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task GenerateContent(string prompt)
        {
            try
            {
                _logger.LogInformation("Generating content for prompt: {Prompt}", prompt);

                // System prompt to guide the AI
                string systemPrompt = @"You are a helpful writing assistant. Generate high-quality, well-structured content based on the user's request.
Keep the tone professional and informative. 
* Format the content with proper paragraphs and make sure all content is generated as an HTML partial document with no attributes on anyof the HTML elements
* If bullet points will contain a title and description, put the title in an h4 and the description in a p
* if generating a code block, use the <pre><code></code></pre> tags
* if generating a table, use the <table class=""spreadsheet""> tag, along with <thead> and <tbody> tags
";
                
                // Empty assistant message since this is the first interaction
                string assistantPrompt = "";

                // Generate content using the LLMs service
                string generatedContent = await LLMs.Prompt(systemPrompt, assistantPrompt, prompt);
                
                // Send the generated content back to the client
                await Clients.Caller.SendAsync("ReceiveContent", generatedContent);
                
                _logger.LogInformation("Content generated and sent to client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content");
                await Clients.Caller.SendAsync("ReceiveContent", $"<p>Error generating content: {ex.Message}</p>");
            }
        }
    }
}
