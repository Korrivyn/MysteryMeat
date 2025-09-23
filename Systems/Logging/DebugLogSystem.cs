using System;
using System.Diagnostics;
using KitchenLib.Logging;
using KitchenMysteryMeat.Enums;

namespace KitchenMysteryMeat.Systems.Logging
{
    /// <summary>
    /// Provides a static logging façade so gameplay systems can emit output that respects the player's debug verbosity.
    /// </summary>
    public static class DebugLogSystem
    {
        /// <summary>
        /// Defines how many helper stack frames should be skipped when building stack traces for log entries.
        /// </summary>
        private const int HelperStackFrameSkip = 2;

        /// <summary>
        /// Caches the Kitchen logger shared by the mod once it becomes available.
        /// </summary>
        private static KitchenLogger _logger;

        /// <summary>
        /// Caches the accessor that resolves the current debug log level preference.
        /// </summary>
        private static Func<DebugLogLevel> _levelProvider;

        /// <summary>
        /// Wires the façade to the Kitchen logger and debug level accessor so all systems log consistently.
        /// </summary>
        /// <param name="logger">The logger provided during mod bootstrap.</param>
        /// <param name="levelProvider">The accessor that resolves the player's chosen debug level.</param>
        public static void Initialise(KitchenLogger logger, Func<DebugLogLevel> levelProvider)
        {
            // Persist the supplied logger so later systems share the same instance.
            if (logger != null)
            {
                _logger = logger;
            }

            // Persist the supplied level accessor so verbosity checks stay centralised.
            if (levelProvider != null)
            {
                _levelProvider = levelProvider;
            }
        }

        /// <summary>
        /// Emits informational updates for major gameplay transitions when debug logging is enabled.
        /// </summary>
        /// <param name="message">The informational message to emit.</param>
        public static void LogInfo(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool isMajorTransition = activeLevel >= DebugLogLevel.On;

            // Guard: only emit informational updates when a logger exists and the player has enabled debug output.
            if (logger != null && isMajorTransition)
            {
                string formattedMessage = FormatMessage(message, isMajorTransition);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Emits warning messages whenever the player has opted into debug logging for issues that may impact gameplay.
        /// </summary>
        /// <param name="message">The warning message to emit.</param>
        public static void LogWarning(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool isWarningEnabled = activeLevel >= DebugLogLevel.On;

            // Guard: only emit warnings when the logger has been resolved and warnings are enabled.
            if (logger != null && isWarningEnabled)
            {
                string formattedMessage = FormatMessage(message, isWarningEnabled);
                logger.LogWarning(formattedMessage);
            }
        }

        /// <summary>
        /// Emits critical failures regardless of the configured verbosity so players can always see fatal issues.
        /// </summary>
        /// <param name="message">The error message to emit.</param>
        public static void LogError(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool includeStackTrace = activeLevel >= DebugLogLevel.On;

            // Guard: only emit when the logger reference is available; errors always log even when verbosity is Off.
            if (logger != null)
            {
                string formattedMessage = FormatMessage(message, includeStackTrace);
                logger.LogError(formattedMessage);
            }
        }

        /// <summary>
        /// Emits verbose diagnostics for deep troubleshooting when the player explicitly requests full verbosity.
        /// </summary>
        /// <param name="message">The verbose message to emit.</param>
        public static void LogVerbose(string message)
        {
            KitchenLogger logger = ResolveLogger();
            DebugLogLevel activeLevel = GetActiveDebugLogLevel();
            bool isVerboseEnabled = activeLevel >= DebugLogLevel.Verbose;

            // Guard: only emit verbose logs when the logger exists and the level is set to Verbose.
            if (logger != null && isVerboseEnabled)
            {
                string formattedMessage = FormatMessage(message, true);
                logger.LogInfo(formattedMessage);
            }
        }

        /// <summary>
        /// Resolves the cached Kitchen logger and stores the shared instance once it becomes available.
        /// </summary>
        /// <returns>The resolved logger instance.</returns>
        private static KitchenLogger ResolveLogger()
        {
            KitchenLogger resolvedLogger = _logger;

            // Guard: fall back to the mod logger when the façade has not been initialised yet.
            if (resolvedLogger == null && Mod.Logger != null)
            {
                resolvedLogger = Mod.Logger;
            }

            _logger = resolvedLogger;
            return resolvedLogger;
        }

        /// <summary>
        /// Resolves the active debug log level so verbosity decisions stay consistent.
        /// </summary>
        /// <returns>The active debug log level.</returns>
        private static DebugLogLevel GetActiveDebugLogLevel()
        {
            Func<DebugLogLevel> resolvedLevelProvider = _levelProvider;

            // Guard: adopt the mod's accessor when initialise has not been called yet.
            if (resolvedLevelProvider == null)
            {
                resolvedLevelProvider = () => Mod.ActiveDebugLogLevel;
                _levelProvider = resolvedLevelProvider;
            }

            DebugLogLevel activeLevel = resolvedLevelProvider.Invoke();
            return activeLevel;
        }

        /// <summary>
        /// Formats messages and appends stack traces when verbosity requires deeper diagnostics.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="includeStackTrace">Indicates whether a stack trace should be appended.</param>
        /// <returns>The formatted message ready for logging.</returns>
        private static string FormatMessage(string message, bool includeStackTrace)
        {
            string formattedMessage = message ?? string.Empty;

            // Guard: only append stack traces when the verbosity level requires additional diagnostics.
            if (includeStackTrace)
            {
                StackTrace stackTrace = new StackTrace(HelperStackFrameSkip, true);
                string trimmedStackTrace = stackTrace.ToString().Trim();

                // Guard: append the stack trace only when it has meaningful content after trimming.
                if (!string.IsNullOrWhiteSpace(trimmedStackTrace))
                {
                    formattedMessage = $"{formattedMessage}{Environment.NewLine}{trimmedStackTrace}";
                }
            }

            return formattedMessage;
        }
    }
}
