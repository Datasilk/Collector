namespace Collector.Common
{
    /// <summary>
    /// Static configuration for Ollama LLM settings
    /// </summary>
    public static class LLMOllama
    {
        /// <summary>
        /// Ollama server URL (default: http://localhost:11434)
        /// </summary>
        public static string Url { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Model to use for reasoning (default: qwen2.5:0.5b)
        /// </summary>
        public static string Model { get; set; } = "qwen2.5:0.5b";

        /// <summary>
        /// Whether to use GPU acceleration
        /// </summary>
        public static bool UseGpu { get; set; } = false;

        /// <summary>
        /// Number of GPUs to use
        /// </summary>
        public static int NumGpu { get; set; } = 1;

        /// <summary>
        /// Context window size in tokens (2048, 4096, 8192, or 32768)
        /// </summary>
        public static int ContextSize { get; set; } = 2048;

        /// <summary>
        /// Sampling temperature (0.0-1.0)
        /// </summary>
        public static float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Automatically pull model if not available
        /// </summary>
        public static bool AutoPullModel { get; set; } = true;
    }
}
