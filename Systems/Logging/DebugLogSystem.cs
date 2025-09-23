using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides an opinionated logging facade that honours the configured debug level and enriches
    /// messages with the Mystery Meat prefix and optional stack traces.
    /// </summary>
    public static class DebugLogSystem
    {
        private const string LogPrefix = "[Mystery Meat]";

        private static KitchenLogger _logger;
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Initialises the logging helper with the mod logger and the active debug level provider.
        /// </summary>
        /// <param name="logger">The logger supplied by the mod framework.</param>
        /// <param name="levelProvider">A delegate that exposes the current debug log level.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            // Store the supplied logger when available so future writes share the reference.
            if (logger != null)
            {
                _logger = logger;
            }

            // Capture the level provider or fall back to the mod accessor when no override is supplied.
            if (levelProvider != null)
            {
                _levelProvider = levelProvider;
            }
            else
            {
                _levelProvider = () => Mod.ActiveDebugLogLevel;
            }
        }

        /// <summary>
        /// Emits an informational log entry when the configured level allows debug output.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public static void LogInfo(string message)
        {
            // Resolve the logger and active level so the method can determine if logging is permitted.
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip logging when the logger is unavailable or informational output is disabled.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                // Include stack traces when the level is at least On to aid diagnostics.
                bool includeStackTrace = activeLevel >= DebugLogLevel.On;
                logger.LogInfo(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Emits a warning log entry when debug logging has been enabled by the user.
        /// </summary>
        /// <param name="message">The warning message to record.</param>
        public static void LogWarning(string message)
        {
            // Resolve the logger and active level so the method can determine if logging is permitted.
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip logging when the logger is unavailable or warning output is disabled.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                // Include stack traces when the level is at least On to aid diagnostics.
                bool includeStackTrace = activeLevel >= DebugLogLevel.On;
                logger.LogWarning(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Emits an error log entry and respects the stack trace preference for verbose diagnostics.
        /// </summary>
        /// <param name="message">The error message to record.</param>
        public static void LogError(string message)
        {
            // Resolve the logger so the method can emit error output when available.
            KitchenLogger logger = ResolveLogger();

            // Guard: skip logging when the logger is unavailable.
            if (logger != null)
            {
                // Include stack traces when the configured level permits expanded diagnostics.
                bool includeStackTrace = GetActiveDebugLogLevel() >= DebugLogLevel.On;
                logger.LogError(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Emits verbose diagnostic output when the debug level has been set to Verbose.
        /// </summary>
        /// <param name="message">The verbose message to record.</param>
        public static void LogVerbose(string message)
        {
            // Resolve the logger and active level so the method can determine if logging is permitted.
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip logging when the logger is unavailable or verbose output is disabled.
            if (logger != null && activeLevel >= DebugLogLevel.Verbose)
            {
                // Verbose logging always includes the stack trace to maximise diagnostic value.
                logger.LogInfo(FormatMessage(message, true));
            }
        }

        /// <summary>
        /// Resolves the logger reference, defaulting to the mod logger when initialisation has not occurred.
        /// </summary>
        /// <returns>The logger instance ready for emitting output, or null if unavailable.</returns>
        private static KitchenLogger ResolveLogger()
        {
            // Synchronise the cached logger with the mod when the helper has not yet been initialised.
            if (_logger == null && Mod.Logger != null)
            {
                _logger = Mod.Logger;
            }

            // Provide the cached logger so callers can emit logs consistently.
            KitchenLogger logger = _logger;
            return logger;
        }

        /// <summary>
        /// Retrieves the active debug logging level from the configured provider.
        /// </summary>
        /// <returns>The debug level currently selected by the user.</returns>
        private static DebugLogLevel GetActiveDebugLogLevel()
        {
            // Default the level provider to the mod accessor when no override has been configured.
            if (_levelProvider == null)
            {
                _levelProvider = () => Mod.ActiveDebugLogLevel;
            }

            // Resolve the level using the configured provider.
            DebugLogLevel activeLevel = _levelProvider.Invoke();
            return activeLevel;
        }

        /// <summary>
        /// Formats messages with the Mystery Meat prefix and optional stack trace details.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for logging.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            // Prepend the log prefix so entries remain searchable within the shared log output.
            string formattedMessage = $"{LogPrefix} {message}";

            // Append the stack trace when verbose diagnostics are requested.
            if (includeStackTrace)
            {
                // Generate a stack trace that skips the logging helper frames for clarity.
                string stackTrace = new StackTrace(2, true).ToString();

                // Append the stack trace only when the generated output contains meaningful content.
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{stackTrace}";
                }
            }

            return formattedMessage;
        }
    }
}
