using System.Text.Json;
using Collector.Common;
using Collector.Common.Plugins;
using CommandCenter.Models;

namespace CommandCenter
{
    public static class App
    {
        public static bool IsDocker { get; set; } = false;
        public static string Environment { get; set; } = "";
        public static string ConfigFilename { get; set; } = "";
        public static Config Config { get; set; } = new Config();
        public static bool Listening { get; set; } = false;
        public static int CharPos { get; set; } = 0;
        public static string UserInput { get; set; } = "";
        public static AudioPlayer AudioPlayer { get; set; }
        public static float Volume { get; set; } = 0.5f;
        private static Random RNG = new Random(new Random().Next());

        #region "Config"
        public static void LoadConfig()
        {
            ConfigFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "config" + (IsDocker ? ".docker" : "") + (Environment == "production" ? ".prod" : "") + ".json"
                );

            if (File.Exists(ConfigFilename))
            {
                try
                {
                    Config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFilename)) ?? new Config();
                    LLMs.PreferredModel = (LLMs.Models)Enum.Parse(typeof(LLMs.Models), Config.PreferredModel);
                    SetOpenAIConfig("Qwen", Config.Qwen);
                    SetOpenAIConfig("ChatGPT", Config.ChatGPT);
                    SetOpenAIConfig("Gemini", Config.Gemini);
                    SetOpenAIConfig("DeepSeek", Config.DeepSeek);
                    TextToSpeech.PrivateKey = Config.ElevenLabs.PrivateKey;
                    TextToSpeech.VoiceId = Config.ElevenLabs.VoiceId;
                    TextToSpeech.VoiceSpeed = Config.ElevenLabs.VoiceSpeed;
                }
                catch (Exception ex) 
                { 
                }
            }
        }

        private static void SetOpenAIConfig(string key, ConfigPrivateKey item)
        {
            try
            {
                var llm = LLMs.Available.Where(a => a.Key.ToString() == key).FirstOrDefault();
                if (llm.Key != LLMs.Models.Unknown) llm.Value.PrivateKey = item.PrivateKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading config values for " + key);
            }
        }
        #endregion

        #region "Plugins"
        public static List<IPlugin> GetPlugins() => new List<IPlugin>()
            {
                new Collector.Common.Plugins.Scholar(),
                new Collector.Plugins.YouTube.Plugin()
            };
        #endregion

        #region "Converse"
        private static List<string> StartingConversation = new List<string>()
        {
            "Okay, let me think about that!"
        };

        public static async Task Converse(string userInput)
        {
                Plugin.Speak(StartingConversation[
                (int)Math.Floor(RNG.NextDouble() * (StartingConversation.Count - 0.0001))]);
            var builder = new PlanBuilder(userInput);
            await builder.Run();
            if (!string.IsNullOrEmpty(builder.Plan.Answer))
            {
                Plugin.Speak(builder.Plan.Answer);
            }
            foreach (var task in builder.Plan.Tasks)
            {
                task.Plugin?.OnExecute(builder.Plan);
            }
        }
        #endregion
    }
}
