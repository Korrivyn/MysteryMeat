/*Script created by Pierre Stempin*/

using UnityEditor;
using UnityEngine;

namespace EmptyAtZeroCreator
{
    /// <summary>
    /// Adds a menu option that spawns an empty child at the global origin beneath the current selection.
    /// </summary>
    public class EmptyChildAtGlobalZero_Creator
    {
        private const string Space = EmptyCreator._Space;
        private const string Global = "Global";
        private const string FeatureName = EmptyCreator.CreateEmptyChildAt_ + Global + Space + EmptyCreator.Zero;
        private const string ShortcutName = EmptyCreator.ControlSymbol + EmptyCreator.AltSymbol + EmptyCreator.ShortcutLetter;
        private const string PathName = EmptyCreator._GameObject + EmptyCreator.Slash + FeatureName + Space + ShortcutName;

        /// <summary>
        /// Creates an empty child aligned to global zero beneath the selected GameObject.
        /// </summary>
        /// <param name="menuCommand">The originating Unity menu command context.</param>
        [MenuItem(PathName, false)]
#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
        public static void CreateEmptyChildAtGlobalZero(MenuCommand menuCommand)
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, false, false, menuCommand);
            EmptyCreator.LogVerbose("EmptyChildAtGlobalZero menu action spawned a child at global zero.");
        }
#else
        public static void CreateEmptyChildAtGlobalZero()
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, false, false);
            EmptyCreator.LogVerbose("EmptyChildAtGlobalZero menu action spawned a child at global zero.");
        }
#endif
    }
}
