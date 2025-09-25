/*Script created by Pierre Stempin*/

using UnityEditor;
using UnityEngine;

namespace EmptyAtZeroCreator
{
    /// <summary>
    /// Adds a menu option that spawns an empty GameObject at the world origin.
    /// </summary>
    public class EmptyAtZero_Creator
    {
        private const string Space = EmptyCreator._Space;
        private const string FeatureName = EmptyCreator.CreateEmpty_ + EmptyCreator.At + Space + EmptyCreator.Zero;
        private const string ShortcutName = EmptyCreator.AltSymbol + EmptyCreator.ShortcutLetter;
        private const string PathName = EmptyCreator._GameObject + EmptyCreator.Slash + FeatureName + Space + ShortcutName;

        [MenuItem(PathName, false, -1)]
#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
        /// <summary>
        /// Spawns an empty GameObject at the origin, deselecting the current context.
        /// </summary>
        /// <param name="menuCommand">The originating Unity menu command context.</param>
        public static void CreateEmptyAtZero(MenuCommand menuCommand)
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, true, false, menuCommand);
            EmptyCreator.LogVerbose("EmptyAtZero menu action spawned a world-origin GameObject.");
        }
#else
        /// <summary>
        /// Spawns an empty GameObject at the origin, deselecting the current context.
        /// </summary>
        public static void CreateEmptyAtZero()
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, true, false);
            EmptyCreator.LogVerbose("EmptyAtZero menu action spawned a world-origin GameObject.");
        }
#endif
    }
}
