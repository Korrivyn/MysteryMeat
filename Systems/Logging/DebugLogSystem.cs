using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Provides helper methods for emitting debug logs with consistent formatting and verbosity control.
    /// </summary>
    public static class DebugLogSystem
    {
        private static KitchenLogger _logger;
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Establishes the logger reference used by the debug log system.
        /// Contributors should invoke this entry point from <see cref="Mod"/> during startup so
        /// that all future logging is routed through the helper and benefits from centralised
        /// formatting and level checks.
        /// </summary>
        /// <param name="logger">The logger instance supplied by the mod host.</param>
        public static void Initialize(KitchenLogger logger)
        {
            // Stores the provided logger once to avoid reassigning a working reference mid-session.
            if (_logger == null && logger != null)
            {
                _logger = logger;
            }
        }

        /// <summary>
        /// Configures the log system with the current logger instance and active debug level provider.
        /// </summary>
        /// <param name="logger">The logger instance used for writing mod output.</param>
        /// <param name="levelProvider">A provider that returns the active debug logging level.</param>
        public static void Configure(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            // Stores the logger and level provider so log calls can use the most recent configuration.
            Initialize(logger);

            // Captures the supplied level provider so callers can override the default accessor.
            if (levelProvider != null)
            {
                _levelProvider = levelProvider;
            }
        }

        /// <summary>
        /// Determines whether a message should be logged based on the configured debug level.
        /// </summary>
        /// <param name="level">The minimum debug level required to emit the message.</param>
        /// <returns>True when the configured level allows logging for the requested message level.</returns>
        public static bool ShouldLog(DebugLogLevel level)
        {
            // Determines whether logging should occur without exposing the configured level to callers.
            DebugLogLevel configuredLevel;
            // Evaluates the logging decision using the overload that also returns the active level.
            bool canLog = ShouldLog(level, out configuredLevel);
            return canLog;
        }

        /// <summary>
        /// Determines whether a message should be logged and outputs the configured level for reuse.
        /// </summary>
        /// <param name="level">The minimum debug level required to emit the message.</param>
        /// <param name="configuredLevel">The active debug level resolved from the provider.</param>
        /// <returns>True when the configured level allows logging for the requested message level.</returns>
        public static bool ShouldLog(DebugLogLevel level, out DebugLogLevel configuredLevel)
        {
            // Retrieves the current debug level for evaluation.
            configuredLevel = GetConfiguredLevel();

            // Evaluates logger readiness and level thresholds in a single consolidated expression.
            bool canLog = _logger != null && configuredLevel != DebugLogLevel.Off && configuredLevel >= level;
            return canLog;
        }

        /// <summary>
        /// Writes an informational message when the debug level permits it.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public static void LogInfo(string message)
        {
            // Determines whether informational logging is permitted at the current debug level.
            bool shouldLog = ShouldLog(DebugLogLevel.On, out DebugLogLevel configuredLevel);

            // Logs the informational message when allowed.
            if (shouldLog)
            {
                // Calculates whether the stack trace should be appended for the current level.
                bool includeStackTrace = configuredLevel >= DebugLogLevel.On;

                // Emits the formatted informational log entry.
                _logger.LogInfo(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Writes a warning message when the debug level permits it.
        /// </summary>
        /// <param name="message">The warning to record.</param>
        public static void LogWarning(string message)
        {
            // Determines whether warning logging is permitted at the current debug level.
            bool shouldLog = ShouldLog(DebugLogLevel.On, out DebugLogLevel configuredLevel);

            // Logs the warning message when allowed.
            if (shouldLog)
            {
                // Calculates whether the stack trace should be appended for the current level.
                bool includeStackTrace = configuredLevel >= DebugLogLevel.On;

                // Emits the formatted warning log entry.
                _logger.LogWarning(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Writes an error message while always respecting logger availability.
        /// </summary>
        /// <param name="message">The error to record.</param>
        public static void LogError(string message)
        {
            // Ensures that error logging only occurs when a logger is configured.
            if (_logger != null)
            {
                // Calculates whether the stack trace should be appended for the current level.
                bool includeStackTrace = GetConfiguredLevel() >= DebugLogLevel.On;

                // Emits the formatted error log entry.
                _logger.LogError(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Writes a verbose message when the debug level is configured for verbose output.
        /// </summary>
        /// <param name="message">The verbose diagnostic to record.</param>
        public static void LogVerbose(string message)
        {
            // Determines whether verbose logging is permitted at the current debug level.
            bool shouldLog = ShouldLog(DebugLogLevel.Verbose, out DebugLogLevel configuredLevel);

            // Logs the verbose message when allowed.
            if (shouldLog)
            {
                // Verbose logging always includes stack traces for detailed diagnostics.
                _logger.LogInfo(FormatMessage(message, true));
            }
        }

        /// <summary>
        /// Retrieves the configured logging level from the registered provider.
        /// </summary>
        /// <returns>The active debug logging level.</returns>
        private static DebugLogLevel GetConfiguredLevel()
        {
            // Begins with the mod accessor so early startup scenarios remain safe when preferences are unavailable.
            DebugLogLevel configuredLevel = Mod.ActiveDebugLogLevel;

            // Allows custom providers to override the accessor when explicitly configured.
            if (_levelProvider != null)
            {
                configuredLevel = _levelProvider.Invoke();
            }

            return configuredLevel;
        }

        /// <summary>
        /// Formats a message with the standard prefix and optional stack trace details.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for logging.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            // Prepends the mod identifier to the message for consistent log parsing.
            string formattedMessage = $"[Mystery Meat] {message}";

            // Appends stack trace details when requested.
            if (includeStackTrace)
            {
                // Generates a stack trace skipping the logging helper frames for clarity.
                string stackTrace = new StackTrace(2, true).ToString();

                // Adds the stack trace to the message when content is available.
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{stackTrace}";
                }
            }

            return formattedMessage;
        }
    }
}
