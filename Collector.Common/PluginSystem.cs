using Collector.Common.Plugins;
using System.Reflection;

namespace Collector.Common
{
    public static class PluginSystem
    {
        private static List<IPlugin> _plugins { get; set; } = new List<IPlugin>();
        private static bool Loaded { get; set; } = false;

        /// <summary>
        /// Gets all IPlugin implementations from all loaded assemblies and from the Plugins folder
        /// </summary>
        /// <returns>List of instantiated plugin objects</returns>
        public static List<IPlugin> GetAll(List<IPlugin>? initialList = null)
        {
            if (_plugins.Count() > 0 || Loaded == true) return _plugins;

            var color = ConsoleColor.DarkGreen;
            var colorReset = ConsoleColor.DarkGray;
            var colorError = ConsoleColor.DarkRed;
            var totalPlugins = initialList != null ? initialList.Count : 0;
            var countPlugins = 0;

            Console.ForegroundColor = colorReset;
            Console.Write("Loading plugins: ");
            Console.ForegroundColor = color;

            if (initialList != null && totalPlugins > 0)
            {
                foreach (var initial in initialList)
                {
                    countPlugins++;
                    Console.Write(initial.Name);
                    if (countPlugins < totalPlugins) Console.Write(", ");
                }
            }

            var list = initialList != null ? initialList : new List<IPlugin>();

            // Get all currently loaded assemblies
            var loadedAssemblies = new List<Assembly>(); //AppDomain.CurrentDomain.GetAssemblies().ToList();
            
            // Load plugins from Plugins directory
            var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (Directory.Exists(pluginsPath))
            {
                var pluginDlls = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                
                foreach (var dllPath in pluginDlls)
                {
                    try
                    {
                        var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                        
                        // Check if the assembly is already loaded
                        if (!loadedAssemblies.Any(a => a.FullName == assemblyName.FullName))
                        {
                            // Load the assembly
                            var assembly = Assembly.LoadFrom(dllPath);
                            loadedAssemblies.Add(assembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception or handle it as needed
                        Console.WriteLine($"Error loading plugin assembly {dllPath}: {ex.Message}");
                    }
                }
            }
            
            // Find all types that implement IPlugin from all assemblies
            foreach (var assembly in loadedAssemblies)
            {
                try
                {
                    // Get all types from the assembly that implement IPlugin
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t))
                        .ToList();

                    if (pluginTypes.Count() == 0) continue;
                    totalPlugins += pluginTypes.Count();

                    // Create an instance of each plugin type
                    foreach (var pluginType in pluginTypes)
                    {
                        countPlugins++;
                        Console.Write(pluginType.Name);
                        try
                        {
                            var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                            if (plugin != null)
                            {
                                list.Add(plugin);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception or handle it as needed
                            Console.ForegroundColor = colorError;
                            Console.Write(" (failed)");
                        }
                        Console.ForegroundColor = colorReset;
                        if (countPlugins < totalPlugins) Console.Write(", ");
                        Console.ForegroundColor = color;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle assembly that couldn't be fully loaded
                    Console.WriteLine($"Error loading types from assembly {assembly.FullName}: {ex.Message}");
                    
                    // You might want to log the LoaderExceptions for more details
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        Console.WriteLine($"  Loader Exception: {loaderEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Error processing assembly {assembly.FullName}: {ex.Message}");
                }
            }
            Console.WriteLine("");
            Console.ForegroundColor = colorReset;
            _plugins = list;
            Loaded = true;

            return list;
        }

        public static void Clear()
        {
            _plugins.Clear();
        }
    }
}
