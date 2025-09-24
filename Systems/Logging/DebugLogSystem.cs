using System;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;
using UnityEngine;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides an opinionated logging facade that honours the configured debug level and appends
    /// optional stack traces while relying on the Kitchen logger for prefix management.
    /// </summary>
    public static class DebugLogSystem
    {
        /// <summary>
        /// Holds a reference to the Kitchen logger supplied by the mod framework.
        /// </summary>
        private static KitchenLogger _logger;

        /// <summary>
        /// Provides access to the current debug log level preference.
        /// </summary>
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Initialises the logging helper with the mod logger and the active debug level provider.
        /// </summary>
        /// <param name="logger">The logger supplied by the mod framework.</param>
        /// <param name="levelProvider">A delegate that exposes the current debug log level.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            // Store the supplied logger reference so future writes can reuse it or clear it when null is supplied.
            _logger = logger;

            // Capture the level provider or fall back to the mod accessor when no override is supplied.
            _levelProvider = levelProvider ?? (() => Mod.ActiveDebugLogLevel);
        }

        /// <summary>
        /// Emits an informational log entry regardless of the configured debug level preference.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public static void LogInfo(string message)
        {
            // Resolve the logger and active level so the method can format the diagnostic output.
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Compute the formatted message once so that both logging paths share the same output.
            string formattedMessage = FormatMessage(message, activeLevel >= DebugLogLevel.On);

            // Guard: log informational output through the Kitchen logger when it is available.
            if (logger != null)
            {
                logger.LogInfo(formattedMessage);
            }
            else
            {
                // Guard: fall back to Unity diagnostics when the Kitchen logger is unavailable.
                Debug.Log(formattedMessage);
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

            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: log warning output through the Kitchen logger when available.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                logger.LogWarning(FormatMessage(message, includeStackTrace));
            }
            else if (activeLevel >= DebugLogLevel.On)
            {
                // Guard: fall back to Unity diagnostics when the Kitchen logger is unavailable.
                Debug.LogWarning(FormatMessage(message, includeStackTrace));
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
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: emit error output through the Kitchen logger when available.
            if (logger != null)
            {
                logger.LogError(FormatMessage(message, includeStackTrace));
            }
            else
            {
                // Guard: fall back to Unity diagnostics when the Kitchen logger is unavailable.
                Debug.LogError(FormatMessage(message, includeStackTrace));
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

            // Guard: skip logging when verbose output is disabled entirely.
            if (activeLevel >= DebugLogLevel.Verbose)
            {
                // Verbose logging always includes the stack trace to maximise diagnostic value.
                string formattedMessage = FormatMessage(message, true);

                // Guard: emit verbose output using the Kitchen logger when available.
                if (logger != null)
                {
                    logger.LogInfo(formattedMessage);
                }
                else
                {
                    // Guard: fall back to Unity diagnostics when the Kitchen logger is unavailable.
                    Debug.Log(formattedMessage);
                }
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
        /// Formats messages and appends optional stack trace details.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for logging.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            // Default null messages to an empty string before formatting output for the logger.
            string formattedMessage = message ?? string.Empty;

            // Append the stack trace when verbose diagnostics are requested.
            if (includeStackTrace)
            {
                // Generate a stack trace that skips the logging helper frames for clarity.
                string stackTrace = new System.Diagnostics.StackTrace(2, true).ToString();

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
