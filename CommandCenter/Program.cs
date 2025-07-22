using Collector.Common;
using CommandCenter;
using CommandCenter.Models;
using System.Text.Json;

Console.WriteLine("Initializing Collector Command Center...");

//check if app is running in Docker Container
App.IsDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

//load config file
App.ConfigFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
    "config" + (App.IsDocker ? ".docker" : "") + (App.Environment == "production" ? ".prod" : "") + ".json"
    );
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("opening config file " + App.ConfigFilename);
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

static void SetOpenAIConfig(string key, ConfigPrivateKey item)
{
    try
    {
        var llm = LLMs.Available.Where(a => a.Key == key).FirstOrDefault();
        if (!string.IsNullOrEmpty(llm.Key)) llm.Value.PrivateKey = item.PrivateKey;
    }
    catch (Exception ex) { }
}


var audio = new AudioPlayer();
var audioFile = TextToSpeech.ConvertTextToSpeech("Collector command center is active. Ask me anything.");
Console.WriteLine("");
Console.ForegroundColor = ConsoleColor.Magenta;
Console.Write("Charlotte: ");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("\"Collector command center is active. Ask me anything.\"");
Console.ResetColor();
Console.WriteLine("");

audio.Play(audioFile);

// Start background spacebar listener
var cancellationTokenSource = new CancellationTokenSource();
var keyListenerTask = Task.Run(() => SpacebarKeyListener(cancellationTokenSource.Token));

// Keep the program running
Console.WriteLine("Press spacebar to talk to Charlotte or press Ctrl+C to exit...");
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    Environment.Exit(0);
};

// Wait for the key listener task or keep program alive
try
{
    await keyListenerTask;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Program terminated.");
}


static async Task SpacebarKeyListener(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Spacebar)
            {
                //toggle listening
                App.Listening = !App.Listening;
                if (App.Listening == true)
                {

                }
            }
        }

        // Small delay to prevent excessive CPU usage
        await Task.Delay(50, cancellationToken);
    }
}