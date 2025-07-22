using OpenAI.Chat;
using Collector.Common.Models;

namespace Collector.Common
{
    public static class LLMs
    {
        public static Dictionary<string, LLMInfo> Available = new Dictionary<string, LLMInfo>()
        {
            {"Qwen", new LLMInfo(){
                Model = "qwen-turbo-latest",
                Endpoint = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1",
                PrivateKey = ""
            }},
            {"ChatGPT", new LLMInfo(){
                Model = "gpt-4o-mini",
                Endpoint = "https://api.openai.com/v1",
                PrivateKey = ""
            }},
            {"Gemini", new LLMInfo(){
                Model = "gemini-2.0-flash-lite",
                Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
                PrivateKey = ""
            }}
        };

        public static string Prompt(string system, string assistant, string user, string llm)
        {
            var myLLM = Available[llm];
            if (string.IsNullOrEmpty(myLLM.PrivateKey))
            {
                throw new Exception("LLM private key is missing");
            }
            ChatClient client = new ChatClient(myLLM.Model, new System.ClientModel.ApiKeyCredential(myLLM.PrivateKey), new OpenAI.OpenAIClientOptions()
            {
                Endpoint = new Uri(myLLM.Endpoint)
            });

            List<ChatMessage> prompt = new List<ChatMessage>()
            {
                new SystemChatMessage(system),
                new AssistantChatMessage(assistant),
                new UserChatMessage(user)
            };
            ChatCompletion completion = client.CompleteChat(prompt);
            return completion.Content[0].Text;
        }
    }
}
