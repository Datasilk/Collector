using Collector.Common;
using CommandCenter.Models;
using System.Text.Json;

namespace CommandCenter
{
    public static class App
    {
        public static bool IsDocker { get; set; } = false;
        public static string Environment { get; set; } = "";
        public static string ConfigFilename { get; set; } = "";
        public static Config Config { get; set; } = new Config();
        public static bool Listening { get; set; } = false;

        public static void LoadConfig() {
            App.ConfigFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "config" + (App.IsDocker ? ".docker" : "") + (App.Environment == "production" ? ".prod" : "") + ".json"
                );

            if (File.Exists(App.ConfigFilename))
            {
                try
                {
                    App.Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(App.ConfigFilename)) ?? new Config();
                    SetOpenAIConfig("Qwen", App.Config.Qwen);
                    SetOpenAIConfig("ChatGPT", App.Config.ChatGPT);
                    SetOpenAIConfig("Gemini", App.Config.Gemini);
                    SetOpenAIConfig("DeepSeek", App.Config.DeepSeek);
                    TextToSpeech.PrivateKey = App.Config.ElevenLabs.PrivateKey;
                }
                catch (Exception ex) { }
            }
        }

        private static void SetOpenAIConfig(string key, ConfigPrivateKey item)
        {
            try
            {
                var llm = LLMs.Available.Where(a => a.Key == key).FirstOrDefault();
                if (!string.IsNullOrEmpty(llm.Key)) llm.Value.PrivateKey = item.PrivateKey;
            }
            catch (Exception ex) { }
        }
    }
}
