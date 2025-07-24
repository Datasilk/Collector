namespace Collector.Plugins.YouTube
{
    public class Plugin : IPlugin
    {
        string IPlugin.Name => "YouTube";

        string IPlugin.Description => "Search YouTube for relavant content and build a list of videos to watch";

        decimal IPlugin.Version => 0.1M;

        string IPlugin.Author => "Mark Entingh";

        string IPlugin.AuthorUrl => "https://github.com/Datasilk/Collector";

        string IPlugin.PromptUseCase => "Collect a list of URL links from YouTube.com for videos related to the user's request";
    }
}
