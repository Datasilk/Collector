namespace Collector.Plugins
{
    public interface IPlugin
    {
        public string Name { get; }
        public string Description { get; }
        public decimal Version { get; }
        public string Author { get; }
        public string AuthorUrl { get; }    
        /// <summary>
        /// Text that will be injected into the AI prompt to describe when and how this plugin should be utilized
        /// </summary>
        public string PromptUseCase { get; }
    }
}
