using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides the central logging facade that respects the configured debug level while relying on KitchenLib's logger to stamp the shared "[Mystery Meat]" prefix once.
    /// </summary>
    public static class DebugLogSystem
    {
        /// <summary>
        /// Defines how many helper frames to skip when building stack traces for emitted logs.
        /// </summary>
        private const int HelperStackFrameSkip = 2;

        /// <summary>
        /// Holds a reference to the Kitchen logger supplied by the mod framework.
        /// </summary>
        private static KitchenLogger _logger;

        /// <summary>
        /// Provides access to the current debug log level preference.
        /// </summary>
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Supplies the fallback provider that reads the mod's active debug log level when no override has been configured.
        /// </summary>
        private static readonly Func<DebugLogLevel> DefaultLevelProvider = () => Mod.ActiveDebugLogLevel;

        /// <summary>
        /// Initialises the logging helper with the mod logger and the active debug level provider.
        /// </summary>
        /// <param name="logger">The logger supplied by the mod framework.</param>
        /// <param name="levelProvider">A delegate that exposes the current debug log level.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            KitchenLogger resolvedLogger = _logger ?? Mod.Logger;
            Func<DebugLogLevel> resolvedLevelProvider = _levelProvider ?? DefaultLevelProvider;

            // Adopt the supplied logger when the caller provides an updated reference during bootstrap.
            if (logger != null)
            {
                resolvedLogger = logger;
            }

            // Adopt the caller's level provider so the helper can poll the live preference when available.
            if (levelProvider != null)
            {
                resolvedLevelProvider = levelProvider;
            }

            _logger = resolvedLogger;
            _levelProvider = resolvedLevelProvider;
        }

        /// <summary>
        /// Emits an informational log entry when players have enabled debug output. KitchenLib already prefixes entries with "[Mystery Meat]" so the helper intentionally avoids adding it again.
        /// </summary>
        /// <param name="message">The message to record.</param>
        public static void LogInfo(string message)
        {
            ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel);

            // Guard: only emit informational logs when a logger exists and the level is set to On or higher.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                string formattedMessage = FormatMessage(message, activeLevel >= DebugLogLevel.On);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Emits a warning log entry when debug logging has been enabled. KitchenLib already prefixes entries with "[Mystery Meat]" so the helper intentionally avoids adding it again.
        /// </summary>
        /// <param name="message">The warning message to record.</param>
        public static void LogWarning(string message)
        {
            ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel);

            // Guard: only emit warning logs when a logger exists and the level is set to On or higher.
            if (logger != null && activeLevel >= DebugLogLevel.On)
            {
                string formattedMessage = FormatMessage(message, activeLevel >= DebugLogLevel.On);
                logger.LogWarning(formattedMessage);
            }
        }

        /// <summary>
        /// Emits an error log entry for critical failures regardless of player verbosity, avoiding extra prefixes because KitchenLib already includes "[Mystery Meat]".
        /// </summary>
        /// <param name="message">The error message to record.</param>
        public static void LogError(string message)
        {
            ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel);

            // Guard: only emit errors when a logger reference is available; errors always log even when verbosity is Off.
            if (logger != null)
            {
                string formattedMessage = FormatMessage(message, activeLevel >= DebugLogLevel.On);
                logger.LogError(formattedMessage);
            }
        }

        /// <summary>
        /// Emits verbose diagnostic output when players request full verbosity, relying on KitchenLib for the shared prefix rather than duplicating it.
        /// </summary>
        /// <param name="message">The verbose message to record.</param>
        public static void LogVerbose(string message)
        {
            ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel);

            // Guard: only emit verbose logs when the logger exists and the level is explicitly set to Verbose.
            if (logger != null && activeLevel >= DebugLogLevel.Verbose)
            {
                string formattedMessage = FormatMessage(message, true);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Emits verbose diagnostic output using a deferred message builder when players opt into the Verbose level.
        /// </summary>
        /// <param name="messageBuilder">A delegate that builds the verbose message when logging is permitted.</param>
        public static void LogVerbose(Func<string> messageBuilder)
        {
            ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel);

            // Guard: bail out when verbose logging is disabled or the logger reference is missing so expensive message construction can be skipped.
            if (logger != null && activeLevel >= DebugLogLevel.Verbose)
            {
                string message = messageBuilder != null ? messageBuilder.Invoke() : string.Empty;
                string formattedMessage = FormatMessage(message, true);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Resolves the cached logger and active level in a single place so callers can make consistent decisions.
        /// </summary>
        /// <param name="logger">Outputs the logger instance ready for emitting output, or null if unavailable.</param>
        /// <param name="activeLevel">Outputs the debug level currently selected by the player.</param>
        private static void ResolveLoggingContext(out KitchenLogger logger, out DebugLogLevel activeLevel)
        {
            KitchenLogger resolvedLogger = _logger;
            Func<DebugLogLevel> resolvedLevelProvider = _levelProvider ?? DefaultLevelProvider;

            // Adopt the mod logger when the helper has not yet cached a reference but the mod has completed initialisation.
            if (resolvedLogger == null && Mod.Logger != null)
            {
                resolvedLogger = Mod.Logger;
            }

            logger = resolvedLogger;
            activeLevel = resolvedLevelProvider.Invoke();

            _logger = resolvedLogger;
            _levelProvider = resolvedLevelProvider;
        }

        /// <summary>
        /// Formats messages and appends optional stack trace details while trimming helper frames.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for logging.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            string formattedMessage = message ?? string.Empty;

            // Append the stack trace when verbose diagnostics are requested and trim helper frames for clarity.
            if (includeStackTrace)
            {
                string stackTrace = new StackTrace(HelperStackFrameSkip, true).ToString().Trim();

                // Guard: only append stack trace output when the captured string has meaningful content.
                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{stackTrace}";
                }
            }

            return formattedMessage;
        }
    }
}
