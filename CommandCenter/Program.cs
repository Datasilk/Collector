using Collector.Common;
using Collector.Common.Plugins;
using CommandCenter;

Console.WriteLine("Initializing Collector Command Center...");

//check if app is running in Docker Container
App.IsDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

//load config file
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("opening config file " + App.ConfigFilename);
App.LoadConfig();

//load plugin system
PluginSystem.GetAll(App.GetPlugins());
Plugin.ConverseListener = OnConverse;

//load audio engine
Console.WriteLine("Loading audio engine...");
App.AudioPlayer = new AudioPlayer();
Console.ResetColor();

//print welcome message
var audioFile = TextToSpeech.ConvertTextToSpeech("Collector command center is active. Ask me anything.");
WriteSpeech("\"Collector command center is active. Ask me anything.\"");

//play welcome message audio file
App.AudioPlayer.Play(audioFile);

// Start background spacebar listener
var cancellationTokenSource = new CancellationTokenSource();
var keyListenerTask = Task.Run(() => KeyPressListener(cancellationTokenSource.Token));

// Keep the program running
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("Type your question, press shift+spacebar to talk to Charlotte, or press Ctrl+C to exit...");
Console.WriteLine("");
NewUserInputLine();
Console.CancelKeyPress += (sender, e) =>
{
    audioFile = TextToSpeech.ConvertTextToSpeech("Goodbye...");
    App.AudioPlayer.Play(audioFile).Wait();
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    Environment.Exit(0);
};

// Wait for either task to complete or keep program alive
try
{
    await Task.WhenAny(keyListenerTask);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Program terminated.");
}

static void OnConverse(string text)
{
    WriteSpeech(string.Join(" ",
        text.Split("[").Select(a =>
        {
            var items = a.Split("]", 2, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length == 2) return items[1];
            return a;
        })).Replace("  ", " ").Replace("  ", " "));

    //play generated voice track
    var audioFile = TextToSpeech.ConvertTextToSpeech(text);
    App.AudioPlayer.Play(audioFile);
}

static void NewUserInputLine()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("> ");
    Console.ResetColor();
    App.CharPos = 0;
}

static void WriteSpeech(string text)
{
    Console.WriteLine("");
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write("Charlotte: ");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(text);
    Console.ResetColor();
    Console.WriteLine("");
}

static async Task KeyPressListener(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Spacebar && keyInfo.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt) && App.Listening == false)
            {
                //toggle listening
                App.Listening = !App.Listening;
                if (App.Listening == true)
                {
                    Console.WriteLine("Listening...");
                    Task.Delay(3000);
                    NewUserInputLine();
                    App.Listening = false;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                App.Converse(App.UserInput);
                NewUserInputLine();
                App.UserInput = "";
            }
            else if (keyInfo.Modifiers == ConsoleModifiers.None || keyInfo.Modifiers == ConsoleModifiers.Shift)
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    try
                    {

                        if (App.CharPos > 0)
                        {
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            Console.Write(" ");
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            App.CharPos -= 1;
                            App.UserInput = App.UserInput.Substring(0, App.UserInput.Length - 1);
                        }
                    }
                    catch (Exception ex)
                  {

                    }
                }
                else
                {
                    Console.ResetColor();
                    Console.Write(keyInfo.KeyChar);
                    App.UserInput += keyInfo.KeyChar;
                    App.CharPos++;
                }
            }
        }

        // Small delay to prevent excessive CPU usage
        await Task.Delay(25, cancellationToken);
    }
}