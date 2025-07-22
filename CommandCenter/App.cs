namespace CommandCenter
{
    public static class App
    {
        public static bool IsDocker { get; set; } = false;
        public static string Environment { get; set; } = "";
        public static string ConfigFilename { get; set; } = "";
        public static Models.Config Config { get; set; } = new Models.Config();
        public static bool Listening { get; set; } = false;
    }
}
