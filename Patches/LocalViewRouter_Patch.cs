using HarmonyLib;
using Kitchen;
using KitchenMysteryMeat.Systems.Logging;
using KitchenMysteryMeat.Views;
using UnityEngine;

namespace KitchenMysteryMeat.Patches
{
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPostfix]
        /// <summary>
        /// Injects the suspicion indicator view onto customer prefabs once the base game provides them.
        /// </summary>
        /// <param name="__instance">The router invoking the hook.</param>
        /// <param name="view_type">The view type being requested.</param>
        /// <param name="__result">The prefab returned by the router.</param>
        static void GetPrefab_Postfix(ref LocalViewRouter __instance, ViewType view_type, ref GameObject __result)
        {
            // Guard: ensure the asset bundle is available before attempting to instantiate custom indicators.
            if (Mod.Bundle == null)
            {
                if (!_missingBundleWarningLogged)
                {
                    DebugLogSystem.LogVerbose("Mystery Meat skipped suspicion indicator injection because the asset bundle is unavailable.");
                    _missingBundleWarningLogged = true;
                }

                return;
            }

            // Guard: only attach the indicator when targeting customer prefabs that have not already been decorated.
            if ((view_type == ViewType.Customer || view_type == ViewType.CustomerCat) && __result != null && __result.GetComponentInChildren<SuspicionIndicatorView>() == null)
            {
                GameObject indicatorPrefab = Mod.Bundle.LoadAsset<GameObject>("SuspicionIndicator");

                // Guard: abort when the indicator prefab is missing from the bundle to avoid null references.
                if (indicatorPrefab == null)
                {
                    DebugLogSystem.LogWarning("Mystery Meat could not locate the SuspicionIndicator prefab while injecting customer views.");
                    return;
                }

                GameObject indicator = Object.Instantiate(indicatorPrefab);
                SuspicionIndicatorView indicatorView = indicator.AddComponent<SuspicionIndicatorView>();
                indicatorView.SuspicionClip = Mod.Bundle.LoadAsset<AudioClip>("suspicion.ogg");
                indicator.transform.SetParent(__result.transform);
            }
        }

        /// <summary>
        /// Tracks whether the asset bundle warning has already been logged.
        /// </summary>
        private static bool _missingBundleWarningLogged;
    }
}
