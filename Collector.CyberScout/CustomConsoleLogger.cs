using Microsoft.Extensions.Logging;
using System;

namespace Collector.CyberScout
{
    // Custom console logger that only shows the message without any prefixes
    public class CustomConsoleLogger : ILogger
    {
        private readonly string _name;

        public CustomConsoleLogger(string name)
        {
            _name = name;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Only output the message without any prefixes
            Console.WriteLine(formatter(state, exception));
        }
    }

    // Custom logger provider
    public class CustomConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new CustomConsoleLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }

    // Extension method to add our custom console logger
    public static class CustomConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddCustomConsole(this ILoggingBuilder builder)
        {
            builder.AddProvider(new CustomConsoleLoggerProvider());
            return builder;
        }
    }
}
