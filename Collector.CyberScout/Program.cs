using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Collector.Data.Interfaces;

namespace Collector.CyberScout
{

    public class Program
    {
        
        private static bool _stopRequested = false;
        
        public static async Task Main(string[] args)
        {
            // Setup dependency injection
            var serviceProvider = ConfigureServices();
            
            // Get logger
            App.Logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            // Get Collector.Data services
            App.DownloadsRepository = serviceProvider.GetRequiredService<IDownloadsRepository>();
            App.DomainsRepository = serviceProvider.GetRequiredService<IDomainsRepository>();
            App.BlacklistsRepository = serviceProvider.GetRequiredService<IBlacklistsRepository>();
            App.ArticlesRepository = serviceProvider.GetRequiredService<IArticlesRepository>();
            App.FeedsRepository = serviceProvider.GetRequiredService<IFeedsRepository>();

            // Check command line arguments
            bool autoStartScout = args.Contains("--scout");
            bool checkFeeds = args.Contains("--feeds");
            
            if (autoStartScout)
            {
                // Automatically start the download queue
                Console.WriteLine("Starting queue check mode...");
                Console.WriteLine("Press ESC to stop the queue processing.");
                
                // Setup key listener for ESC key
                SetupKeyListener();
                
                // Keep checking queue until stopped
                while (!_stopRequested)
                {
                    await Scout.CheckQueue(Guid.NewGuid().ToString(), 0, "", 0);
                    
                    // Small delay between checks
                    await Task.Delay(1000);
                }
            }
            else if (checkFeeds)
            {
                // Automatically check all feeds
                Console.WriteLine("Starting feed check mode...");
                Console.WriteLine("Press ESC to stop the feed checking.");
                
                // Setup key listener for ESC key
                SetupKeyListener();
                
                await Feeds.CheckFeeds(0); // 0 means check all feeds
            }
            else
            {
                // Interactive command mode
                await RunCommandMode();
            }
        }
        
        private static void SetupKeyListener()
        {
            // Reset the stop flag before setting up a new listener
            _stopRequested = false;
            
            // Start a background task to listen for ESC key
            Task.Run(() => {
                while (!_stopRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            _stopRequested = true;
                            Console.WriteLine("\nTask canceled by user.");
                            break;
                        }
                    }
                    
                    // Small delay to prevent high CPU usage
                    Thread.Sleep(100);
                }
            });
        }
        
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            services.AddSingleton<IConfiguration>(configuration);
            
            // Configure logging to use our custom console logger that only shows messages without prefixes
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddCustomConsole();
            });
            
            // Add database connection
            string connectionString = configuration.GetConnectionString("Database");
            services.AddTransient<IDbConnection>((sp) => new SqlConnection(connectionString));
            
            // Add repositories
            services.AddTransient<IDownloadsRepository, Data.Repositories.DownloadsRepository>();
            services.AddTransient<IDomainsRepository, Data.Repositories.DomainsRepository>();
            services.AddTransient<IBlacklistsRepository, Data.Repositories.BlacklistsRepository>();
            services.AddTransient<IArticlesRepository, Data.Repositories.ArticlesRepository>();
            services.AddTransient<IFeedsRepository, Data.Repositories.FeedsRepository>();
            
            return services.BuildServiceProvider();
        }

        private static async Task RunCommandMode()
        {
            Console.WriteLine("CyberScout Command Mode");
            Console.WriteLine("Type 'scout' to start the download queue");
            Console.WriteLine("Type 'feeds' to check all feeds");
            Console.WriteLine("Type 'exit' to quit");
            Console.WriteLine();

            bool running = true;
            while (running)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower() ?? "";

                switch (command)
                {
                    case "scout":
                        Console.WriteLine("Starting download queue...");
                        Console.WriteLine("Press ESC to stop the queue processing.");
                        
                        // Reset stop flag
                        _stopRequested = false;
                        
                        // Setup key listener for ESC key
                        SetupKeyListener();
                        
                        // Process queue until ESC is pressed
                        while (!_stopRequested)
                        {
                            await Scout.CheckQueue(Guid.NewGuid().ToString(), 0, "", 0);
                            await Task.Delay(1000);
                        }
                        break;

                    case "feeds":
                        Console.WriteLine("Checking all feeds...");
                        Console.WriteLine("Press ESC to stop the feed checking.");
                        
                        // Reset stop flag
                        _stopRequested = false;
                        
                        // Setup key listener for ESC key
                        SetupKeyListener();
                        
                        // Check all feeds
                        await Feeds.CheckFeeds(0);
                        break;

                    case "exit":
                        running = false;
                        Console.WriteLine("Exiting CyberScout...");
                        break;

                    case "help":
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("  scout - Start the download queue");
                        Console.WriteLine("  feeds - Check all feeds");
                        Console.WriteLine("  exit  - Exit the application");
                        Console.WriteLine("  help  - Show this help message");
                        break;

                    case "":
                        // Do nothing for empty input
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        Console.WriteLine("Type 'help' for a list of available commands");
                        break;
                }

                Console.WriteLine();
            }
        }
    }
}
