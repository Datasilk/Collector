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
            bool useDomainsSort = args.Contains("--domains");
            
            // Check for domain parameter
            string domain = "";
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--domain")
                {
                    domain = args[i + 1];
                    break;
                }
            }
            
            if (autoStartScout)
            {
                // Automatically start the download queue
                int sortValue = useDomainsSort ? 2 : 3;
                StartDownloadQueueMessage(Guid.NewGuid().ToString(), 0, domain, sortValue);
                Console.WriteLine("Press ESC to stop the queue processing.");
                
                // Setup key listener for ESC key
                SetupKeyListener();
                
                // Keep checking queue until stopped
                while (!_stopRequested)
                {
                    // Use sort=2 when --domains is specified or use sort=3 for random sorting
                    Scout.CheckQueue(Guid.NewGuid().ToString(), 0, domain, sortValue);
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
            
            // Get Services properties from configuration
            Common.Article.BrowserEndpoint = configuration["Charlotte:Router:Endpoint"] ?? "http://localhost:7007/GetDOM";
            Common.Files.ContentPath = configuration["Content:Path"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
            
            return services.BuildServiceProvider();
        }

        private static async Task RunCommandMode()
        {
            List<string> intros = new List<string> { 
                "CyberScout online! Ready to crawl the digital wilderness!",
                "Prepared to parse! CyberScout's web crawlers stand ready for action!",
                "Ready to map digital trails and calibrate RSS compass - CyberScout awaiting deployment!",
                "On my honor, no PDF shall remain unindexed! CyberScout standing by!",
                "Trustworthy data collection is my motto - CyberScout ready to begin responsible scraping!",
                "APIs in sight, feeds in range - CyberScout awaiting your command!",
                "Ready to follow every link! CyberScout prepared to begin digital expedition!",
                "Content campfire ready and crawler cookies prepared - CyberScout ready to harvest!",
                "A good CyberScout always checks robots.txt first - standing by for ethical scraping!",
                "Bytes before bedtime! CyberScout reporting with bandwidth and determination!",
                "Web forest navigation tools calibrated - CyberScout ready to explore!",
                "HTML trails ahead! CyberScout prepared with parsing compass!",
                "Digital merit badges at the ready - CyberScout prepared to earn more data points!",
                "Scraper toolkit unpacked and ready - CyberScout awaiting coordinates!",
                "RSS streams flow with information - CyberScout ready to navigate the currents!",
                "Prepared to venture into the deepest archives - CyberScout ready for expedition!",
                "Crawler bots polished and programmed - CyberScout ready to deploy!",
                "Data campsite established - CyberScout ready to collect digital specimens!",
                "Web knots ready to be untangled - CyberScout reporting with parsing tools!",
                "Digital compass pointing to content - CyberScout ready to follow the needle!",
                "Content extraction tools sharpened - CyberScout ready to carve data paths!",
                "Spider silk loaded and spinnerets ready - CyberScout prepared to weave the web!",
                "Cache empty and memory clear - CyberScout ready for new discoveries!",
                "Bandwidth backpack packed for the journey - CyberScout ready to trek!",
                "Firewall climbing gear secured - CyberScout ready to scale digital mountains!",
                "API whistle tuned to the right frequency - CyberScout ready to call for data!",
                "Digital campfire stories ready to be collected - CyberScout eager to listen!",
                "Web tracking skills honed and ready - CyberScout prepared to follow any trail!",
                "Content filtering canteen filled - CyberScout won't go thirsty for relevant data!",
                "Scraping knife sharpened to precision - CyberScout ready to carve out content!",
                "Digital first aid kit packed for 404 emergencies - CyberScout prepared for anything!",
                "Web compass pointing true north - CyberScout ready to navigate the information forest!",
                "Content snares set with care - CyberScout ready to trap valuable information!",
                "Parser engines warmed up and humming - CyberScout ready to process!",
                "Digital binoculars focused on the horizon - CyberScout ready to spot new content!",
                "Web mapping tools calibrated - CyberScout ready to chart unexplored domains!",
                "Content fishing line cast into the data stream - CyberScout ready to reel in results!",
                "Crawler boots laced tight - CyberScout ready to trek through server jungles!",
                "Digital merit badges gleaming on virtual sash - CyberScout ready to earn more!",
                "Web knots ready to be tied - CyberScout prepared to connect the data points!",
                "Content flint and steel ready to spark insights - CyberScout prepared to light the way!",
                "Scraper toolkit organized and accessible - CyberScout ready for any extraction challenge!",
                "Digital trail mix packed with parsing energy - CyberScout ready for the long haul!",
                "Web whistle at the ready - CyberScout prepared to call for backup data!",
                "Content compass aligned with true information - CyberScout ready to avoid fake news!",
                "Crawler canteen filled with persistence - CyberScout won't stop until the job is done!",
                "Digital rope coiled and ready - CyberScout prepared to rappel into data depths!",
                "Web tent stakes hammered into reliable sources - CyberScout base camp established!",
                "Content signal fire ready to be lit - CyberScout prepared to highlight findings!",
                "Parser pocketknife sharpened to a fine edge - CyberScout ready to slice through data!",
                "Digital flashlight batteries fresh - CyberScout ready to illuminate dark web corners!",
                "Web tracking skills ready to be tested - CyberScout eager to follow the data spoor!",
                "Content backpack adjusted for optimal carrying - CyberScout ready to haul information!",
                "Crawler boots waterproofed against data floods - CyberScout prepared for any conditions!",
                "Digital map unfolded and ready - CyberScout prepared to chart the content territory!",
                "Web campsite selected for optimal coverage - CyberScout ready to begin operations!",
                "Content filtering system primed and ready - CyberScout prepared to separate signal from noise!",
                "Parser engines revving with anticipation - CyberScout ready to process the web!",
                "Digital compass pointing to valuable data - CyberScout ready to follow the needle!",
                "Web harvesting tools sharpened and ready - CyberScout prepared to reap information!",
                "Content extraction protocols initialized - CyberScout ready to begin collection!"
            };
            // Display a random intro message
            Random random = new Random();
            Console.WriteLine(intros[random.Next(intros.Count)]);
            ShowHelp();

            bool running = true;
            while (running)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower() ?? "";

                // Parse command and arguments
                string[] commandParts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string mainCommand = commandParts.Length > 0 ? commandParts[0] : "";
                
                // Handle scout command with or without parameters
                if (mainCommand == "scout")
                {
                    string domain = "";
                    bool useDomainsSort = false;
                    
                    // Check for --domain parameter
                    for (int i = 1; i < commandParts.Length - 1; i++)
                    {
                        if (commandParts[i] == "--domain")
                        {
                            domain = commandParts[i + 1];
                            break;
                        }
                    }
                    
                    // Check for --domains parameter
                    useDomainsSort = commandParts.Contains("--domains");

                    // Build message based on parameters
                    int sortValue = useDomainsSort ? 2 : 3; // Use sort=2 when --domains is specified
                    StartDownloadQueueMessage(Guid.NewGuid().ToString(), 0, domain, sortValue);
                    Console.WriteLine("Press ESC to stop the queue processing.");
                    
                    // Reset stop flag
                    _stopRequested = false;
                    
                    // Setup key listener for ESC key
                    SetupKeyListener();
                    
                    // Process queue until ESC is pressed
                    while (!_stopRequested)
                    {
                        Scout.CheckQueue(Guid.NewGuid().ToString(), 0, domain, sortValue);
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    switch (command)
                    {
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
                            ShowHelp();
                            break;

                        case "":
                            // Do nothing for empty input
                            break;

                        default:
                            Console.WriteLine($"Unknown command: {command}");
                            Console.WriteLine("Type 'help' for a list of available commands");
                            break;
                    }
                }

                Console.WriteLine();
            }
        }

        private static void StartDownloadQueueMessage(string id, int feedId, string domainName, int sort)
        {
            var sortName = sort == 0 ? "newest" : sort == 1 ? "oldest" : sort == 2 ? "domain" : sort == 3 ? "random" : "unknown";
            var feedName = feedId > 0 ? " feed Id " + feedId : "";
            App.Logger.LogInformation("Starting download queue for {feed}{domain} sorting by {status}!",
                feedName + (feedName != "" && domainName != "" ? " & " : ""),
                domainName + (domainName != "" ? ", " : ""), sortName);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Type 'scout' to start the download queue");
            Console.WriteLine("Type 'scout --domain example.com' to start the download queue for a domain");
            Console.WriteLine("Type 'scout --domains' to start downloading the home page of new domains");
            Console.WriteLine("Type 'feeds' to check all feeds");
            Console.WriteLine("Type 'exit' to quit");
            Console.WriteLine();
        }
    }
}
