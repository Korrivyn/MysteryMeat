using HarmonyLib;
using Kitchen;
using Kitchen.Components;
using KitchenMysteryMeat.MonoBehaviours;
using KitchenMysteryMeat.Systems.Logging;
using KitchenMysteryMeat.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat.Patches
{
    /// <summary>
    /// Harmonises the sound event view to apply preference-aware volume adjusters to Mystery Meat clips.
    /// </summary>
    [HarmonyPatch]
    static class SoundEventView_Patch
    {
        [HarmonyPatch(typeof(SoundEventView), "UpdateData")]
        [HarmonyPrefix]
        /// <summary>
        /// Ensures Mystery Meat audio events respect player-configured volume preferences.
        /// </summary>
        static bool UpdateData_Prefix(ref SoundEventView __instance, SoundEventView.ViewData data)
        {
            if (data.Event == Mod.AlertSoundEvent)
            {
                __instance.gameObject.AddComponent<PreferenceVolumeAdjuster>().PreferenceID = Mod.ALERT_VOLUME_ID;
                DebugLogSystem.LogVerbose("SoundEventView_Patch applied alert volume preference adjuster.");

            }
            else if (data.Event == Mod.StabSoundEvent)
            {
                __instance.gameObject.AddComponent<PreferenceVolumeAdjuster>().PreferenceID = Mod.STAB_VOLUME_ID;
                DebugLogSystem.LogVerbose("SoundEventView_Patch applied stab volume preference adjuster.");
            }
            return true;
        }
    }
}
