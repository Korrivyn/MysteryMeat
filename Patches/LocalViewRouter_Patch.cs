using HarmonyLib;
using Kitchen;
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
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPostfix]
        static void GetPrefab_Postfix(ref LocalViewRouter __instance, ViewType view_type, ref GameObject __result)
        {
            if ((view_type == ViewType.Customer || view_type == ViewType.CustomerCat) && __result != null && __result.GetComponentInChildren<SuspicionIndicatorView>() == null)
            {
                GameObject indicator = GameObject.Instantiate(Mod.Bundle.LoadAsset<GameObject>("SuspicionIndicator"));
                SuspicionIndicatorView indicatorView = indicator.AddComponent<SuspicionIndicatorView>();
                indicatorView.SuspicionClip = Mod.Bundle.LoadAsset<AudioClip>("suspicion.ogg");
                indicator.transform.SetParent(__result.transform);
            }
        }
    }
}
