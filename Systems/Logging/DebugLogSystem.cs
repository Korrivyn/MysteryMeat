using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides the centralised logging facade for Mystery Meat, wiring KitchenLib's logger to
    /// configurable debug levels while avoiding duplicate mod prefixes in the output stream.
    /// </summary>
    public static class DebugLogSystem
    {
        /// <summary>
        /// Caches the KitchenLib logger that handles the actual output formatting.
        /// </summary>
        private static KitchenLogger _logger;

        /// <summary>
        /// Supplies the active debug level so logging thresholds can honour player preferences.
        /// </summary>
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Initialises the logging pipeline with the primary logger and the active debug level provider.
        /// </summary>
        /// <param name="logger">The logger supplied by KitchenLib.</param>
        /// <param name="levelProvider">A delegate that exposes the current <see cref="DebugLogLevel"/>.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            KitchenLogger resolvedLogger = _logger;
            Func<DebugLogLevel> resolvedProvider = _levelProvider;

            // Only update the cached logger when a valid reference is supplied by the caller.
            if (logger != null)
            {
                resolvedLogger = logger;
            }

            // Only update the cached level provider when a new delegate is supplied by the caller.
            if (levelProvider != null)
            {
                resolvedProvider = levelProvider;
            }
            else if (resolvedProvider == null)
            {
                // Fall back to the mod accessor so logging can proceed before initialisation completes.
                resolvedProvider = () => Mod.ActiveDebugLogLevel;
            }

            _logger = resolvedLogger;
            _levelProvider = resolvedProvider;
        }

        /// <summary>
        /// Logs informational state changes when the player has enabled at least the On debug level.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids repeating it.
        /// </summary>
        /// <param name="message">The informational message to record.</param>
        public static void LogInfo(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip informational logging unless a logger exists and the player requested On or higher verbosity.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                bool includeStackTrace = activeLevel >= DebugLogLevel.On;
                logger.LogInfo(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Logs warnings when the player has opted into the On or Verbose debug levels.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids repeating it.
        /// </summary>
        /// <param name="message">The warning message to record.</param>
        public static void LogWarning(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip warning logging unless a logger exists and the player requested On or higher verbosity.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                bool includeStackTrace = activeLevel >= DebugLogLevel.On;
                logger.LogWarning(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Logs errors regardless of the configured debug level so critical faults are always surfaced to players.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids repeating it.
        /// </summary>
        /// <param name="message">The error message to record.</param>
        public static void LogError(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            // Guard: skip error logging only when no logger is available; Off still permits critical output.
            if (logger != null)
            {
                bool includeStackTrace = activeLevel >= DebugLogLevel.On;
                logger.LogError(FormatMessage(message, includeStackTrace));
            }
        }

        /// <summary>
        /// Logs verbose diagnostic breadcrumbs when the player has set the debug level to Verbose.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids repeating it.
        /// </summary>
        /// <param name="message">The verbose message to record.</param>
        public static void LogVerbose(string message)
        {
            LogVerbose(() => message);
        }

        /// <summary>
        /// Logs verbose diagnostic breadcrumbs via a deferred message provider when Verbose output is enabled.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids repeating it.
        /// </summary>
        /// <param name="messageProvider">A delegate that supplies the verbose message.</param>
        public static void LogVerbose(Func<string> messageProvider)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();

            bool canLog = logger != null && activeLevel >= DebugLogLevel.Verbose;

            // Guard: ensures verbose writes only run when the logger is ready and the player requested Verbose diagnostics.
            if (canLog)
            {
                string message = messageProvider != null ? messageProvider() : string.Empty;
                logger.LogInfo(FormatMessage(message, true));
            }
        }

        /// <summary>
        /// Resolves the logger reference, defaulting to the mod logger when the helper has not yet been initialised.
        /// </summary>
        /// <returns>The logger ready to receive output, or null if unavailable.</returns>
        private static KitchenLogger ResolveLogger()
        {
            KitchenLogger resolvedLogger = _logger;

            // Adopt the mod logger automatically when the helper has yet to capture an explicit reference.
            if (resolvedLogger == null && Mod.Logger != null)
            {
                resolvedLogger = Mod.Logger;
            }

            _logger = resolvedLogger;
            return resolvedLogger;
        }

        /// <summary>
        /// Retrieves the active debug level using the cached provider or the mod fallback when uninitialised.
        /// </summary>
        /// <returns>The debug logging level selected by the player.</returns>
        private static DebugLogLevel GetActiveDebugLogLevel()
        {
            Func<DebugLogLevel> resolvedProvider = _levelProvider;

            // Fall back to the mod accessor when no level provider has been configured yet.
            if (resolvedProvider == null)
            {
                resolvedProvider = () => Mod.ActiveDebugLogLevel;
            }

            DebugLogLevel activeLevel = resolvedProvider.Invoke();
            _levelProvider = resolvedProvider;
            return activeLevel;
        }

        /// <summary>
        /// Formats log messages and optionally appends stack trace details that skip helper frames.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for output.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            string formattedMessage = message ?? string.Empty;

            // Append the stack trace when the caller requested deeper diagnostics.
            if (includeStackTrace)
            {
                string stackTrace = BuildTrimmedStackTrace();

                // Append the trimmed stack trace only when it contains meaningful content.
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{stackTrace}";
                }
            }

            return formattedMessage;
        }

        /// <summary>
        /// Builds a stack trace string that omits the logging helper frames for clarity.
        /// </summary>
        /// <returns>A trimmed stack trace string, or an empty string when unavailable.</returns>
        private static string BuildTrimmedStackTrace()
        {
            StackTrace trace = new StackTrace(2, true);
            string traceText = trace.ToString()?.Trim();

            string trimmedTrace = string.IsNullOrEmpty(traceText) ? string.Empty : traceText;
            return trimmedTrace;
        }
    }
}
