using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides a static logging facade so gameplay systems can respect the player's debug verbosity preferences while still
    /// funnelling messages through KitchenLib's shared Mystery Meat logger.
    /// </summary>
    public static class DebugLogSystem
    {
        /// <summary>
        /// Defines how many helper frames to omit when including stack traces alongside emitted log messages.
        /// </summary>
        private const int HelperStackFrameSkip = 2;

        /// <summary>
        /// Caches the Kitchen logger supplied by the mod framework so all systems share a single output channel.
        /// </summary>
        private static KitchenLogger _logger;

        /// <summary>
        /// Stores the accessor responsible for resolving the active debug log level from mod preferences.
        /// </summary>
        private static Func<DebugLogLevel> _levelAccessor;

        /// <summary>
        /// Provides the default accessor that queries the mod's live debug log level when no override has been configured.
        /// </summary>
        private static readonly Func<DebugLogLevel> DefaultLevelAccessor = () => Mod.ActiveDebugLogLevel;

        /// <summary>
        /// Initialises the logging helper with the shared logger and the accessor used to evaluate the player's debug preference.
        /// </summary>
        /// <param name="logger">The Kitchen logger supplied during mod bootstrap.</param>
        /// <param name="levelAccessor">A delegate that returns the current debug log level.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelAccessor)
        {
            KitchenLogger fallbackLogger = _logger ?? Mod.Logger;
            Func<DebugLogLevel> fallbackLevelAccessor = _levelAccessor ?? DefaultLevelAccessor;

            // Cache the resolved logger so every gameplay system writes through the same KitchenLib instance.
            _logger = logger ?? fallbackLogger;

            // Cache the resolved accessor so future log evaluations continue to respect the player's preference.
            _levelAccessor = levelAccessor ?? fallbackLevelAccessor;
        }

        /// <summary>
        /// Emits informational updates describing major gameplay transitions whenever players enable debug logging.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids duplicating it.
        /// </summary>
        /// <param name="message">The informational message to record.</param>
        public static void LogInfo(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool shouldLog = logger != null && activeLevel >= DebugLogLevel.On;
            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: only emit informational logs when a logger exists and players requested visibility into major transitions.
            if (shouldLog)
            {
                string formattedMessage = FormatMessage(message, includeStackTrace);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Emits warnings that highlight risky or suspicious gameplay scenarios while respecting the configured verbosity.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids duplicating it.
        /// </summary>
        /// <param name="message">The warning message to record.</param>
        public static void LogWarning(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool shouldLog = logger != null && activeLevel >= DebugLogLevel.On;
            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: only emit warnings when a logger exists and players enabled diagnostic logging for elevated events.
            if (shouldLog)
            {
                string formattedMessage = FormatMessage(message, includeStackTrace);
                logger.LogWarning(formattedMessage);
            }
        }

        /// <summary>
        /// Emits error logs for critical gameplay failures even when players disable verbose output.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids duplicating it.
        /// </summary>
        /// <param name="message">The error message to record.</param>
        public static void LogError(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool shouldLog = logger != null;
            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: only emit errors when a logger reference has been resolved during the bootstrap sequence.
            if (shouldLog)
            {
                string formattedMessage = FormatMessage(message, includeStackTrace);
                logger.LogError(formattedMessage);
            }
        }

        /// <summary>
        /// Emits verbose diagnostics that expose fine-grained state changes whenever the debug log level is set to Verbose.
        /// KitchenLib already prefixes entries with "[Mystery Meat]", so this helper intentionally avoids duplicating it.
        /// </summary>
        /// <param name="message">The verbose message to record.</param>
        public static void LogVerbose(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool shouldLog = logger != null && activeLevel >= DebugLogLevel.Verbose;
            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: only emit verbose logs when players explicitly opt into full diagnostic detail.
            if (shouldLog)
            {
                string formattedMessage = FormatMessage(message, includeStackTrace);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Resolves the cached logger so log calls can execute safely even if the helper is initialised late.
        /// </summary>
        /// <returns>The Kitchen logger ready for message emission, or <c>null</c> if bootstrap has not provided one yet.</returns>
        private static KitchenLogger ResolveLogger()
        {
            KitchenLogger resolvedLogger = _logger ?? Mod.Logger;

            // Guard: persist the resolved logger so subsequent calls reuse the same KitchenLib instance.
            if (_logger != resolvedLogger)
            {
                _logger = resolvedLogger;
            }

            return resolvedLogger;
        }

        /// <summary>
        /// Retrieves the active debug log level so systems can evaluate whether to emit additional diagnostic context.
        /// </summary>
        /// <returns>The debug log level currently selected by the player.</returns>
        private static DebugLogLevel GetActiveDebugLogLevel()
        {
            Func<DebugLogLevel> levelAccessor = _levelAccessor ?? DefaultLevelAccessor;

            // Guard: cache the resolved accessor so repeated log calls share the same provider.
            if (_levelAccessor == null)
            {
                _levelAccessor = levelAccessor;
            }

            return levelAccessor.Invoke();
        }

        /// <summary>
        /// Formats messages and appends trimmed stack traces when verbose diagnostics are requested.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">A value indicating whether stack trace information should be appended.</param>
        /// <returns>The formatted message ready for emission.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            string formattedMessage = message ?? string.Empty;

            // Append the stack trace when diagnostic detail is required and trim helper frames for clarity.
            if (includeStackTrace)
            {
                StackTrace stackTrace = new StackTrace(HelperStackFrameSkip, true);
                string trimmedStackTrace = stackTrace.ToString().Trim();

                // Guard: only append stack trace details when the captured string contains meaningful content.
                if (!string.IsNullOrWhiteSpace(trimmedStackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{trimmedStackTrace}";
                }
            }

            return formattedMessage;
        }
    }
}
