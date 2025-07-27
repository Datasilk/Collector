namespace Collector.Common.Plugins
{
    public static class Plugin
    {
        public static Action<string>? ConverseListener { get; set; }

        public static void Speak(this IPlugin plugin, string phraseKey)
        {
            Speak(plugin.SpeechPhrases[phraseKey]);
        }

        public static void Speak(string text)
        {
            if(ConverseListener != null) ConverseListener.Invoke(text);
        }
    }
}
