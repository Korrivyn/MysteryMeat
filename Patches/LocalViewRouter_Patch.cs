﻿using HarmonyLib;
using Kitchen;
using KitchenMysteryMeat.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace KitchenMysteryMeat.Patches
{
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPostfix]
        static void GetPrefab_Postfix(ref LocalViewRouter __instance, ViewType view_type, ref GameObject __result)
        {
            if (view_type == ViewType.Customer && __result != null && __result.GetComponentInChildren<SuspicionIndicatorView>() == null)
            {
                GameObject indicator = GameObject.Instantiate(Mod.Bundle.LoadAsset<GameObject>("SuspicionIndicator"));
                indicator.AddComponent<SuspicionIndicatorView>();
                indicator.transform.SetParent(__result.transform);
            }
        }
    }
}
