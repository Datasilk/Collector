namespace CommandCenter.Models
{
    public class Config
    {
        public ConfigPrivateKey Qwen { get; set; } = new ConfigPrivateKey();
        public ConfigPrivateKey ChatGPT { get; set; } = new ConfigPrivateKey();
        public ConfigPrivateKey Gemini { get; set; } = new ConfigPrivateKey();
        public ConfigPrivateKey DeepSeek { get; set; } = new ConfigPrivateKey();
        public ConfigElevenLabs ElevenLabs { get; set; } = new ConfigElevenLabs();
    }

    public class ConfigPrivateKey
    {
        public string PrivateKey { get; set; } = "";
    }

    public class ConfigElevenLabs: ConfigPrivateKey
    {
        public string VoiceId { get; set; } = "";
    }
}
