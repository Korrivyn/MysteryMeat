/*Script created by Pierre Stempin*/

using UnityEditor;
using UnityEngine;

namespace EmptyAtZeroCreator
{
    /// <summary>
    /// Adds a menu option that spawns an empty child aligned to local zero beneath the current selection.
    /// </summary>
    public class EmptyChildAtLocalZero_Creator
    {
        private const string Space = EmptyCreator._Space;
        private const string Local = "Local";
        private const string FeatureName = EmptyCreator.CreateEmptyChildAt_ + Local + Space + EmptyCreator.Zero;
        private const string ShortcutName = EmptyCreator.ControlSymbol + EmptyCreator.AltSymbol + EmptyCreator.ShiftSymbol + EmptyCreator.ShortcutLetter;
        private const string PathName = EmptyCreator._GameObject + EmptyCreator.Slash + FeatureName + Space + ShortcutName;

        /// <summary>
        /// Creates an empty child reset to local zero beneath the selected GameObject.
        /// </summary>
        /// <param name="menuCommand">The originating Unity menu command context.</param>
        [MenuItem(PathName, false)]
#if UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_2017_1_OR_NEWER
        public static void CreateEmptyChildAtLocalZero(MenuCommand menuCommand)
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, false, true, menuCommand);
            EmptyCreator.LogVerbose("EmptyChildAtLocalZero menu action spawned a child at local zero.");
        }
#else
        public static void CreateEmptyChildAtLocalZero()
        {
            EmptyCreator.CreateEmptyGameObject(FeatureName, false, true);
            EmptyCreator.LogVerbose("EmptyChildAtLocalZero menu action spawned a child at local zero.");
        }
#endif
    }
}
