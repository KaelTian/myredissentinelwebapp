using log4net;
using log4net.Config;

namespace MyRedisSentinelWebApp.Logging
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly string _configFile;

        public Log4NetProvider(string configFile)
        {
            _configFile = configFile;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Log4NetLogger(categoryName, _configFile);
        }

        public void Dispose()
        {
            // Dispose of resources if needed
        }
    }
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;

        public Log4NetLogger(string categoryName, string configFile)
        {
            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("log4net configuration file not found", configFile);
            }

            var logRepository = LogManager.GetRepository(typeof(Log4NetLogger).Assembly);
            if (!logRepository.Configured)
            {
                XmlConfigurator.Configure(logRepository, new FileInfo(configFile));
            }

            _log = LogManager.GetLogger(logRepository.Name, categoryName);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => _log.IsDebugEnabled,
                LogLevel.Debug => _log.IsDebugEnabled,
                LogLevel.Information => _log.IsInfoEnabled,
                LogLevel.Warning => _log.IsWarnEnabled,
                LogLevel.Error => _log.IsErrorEnabled,
                LogLevel.Critical => _log.IsFatalEnabled,
                _ => false,
            };
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _log.Debug(message, exception);
                    break;
                case LogLevel.Information:
                    _log.Info(message, exception);
                    break;
                case LogLevel.Warning:
                    _log.Warn(message, exception);
                    break;
                case LogLevel.Error:
                    _log.Error(message, exception);
                    break;
                case LogLevel.Critical:
                    _log.Fatal(message, exception);
                    break;
            }
        }
    }
}
