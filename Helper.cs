using Kitchen;
using KitchenLib.Utils;
using KitchenMysteryMeat.Systems.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KitchenMysteryMeat
{
    public static class Helper
    {
        /// <summary>
        /// Loads a prefab from the mod asset bundle by the provided asset name.
        /// </summary>
        /// <param name="name">The name of the prefab asset to load.</param>
        /// <returns>The prefab associated with the given name.</returns>
        public static GameObject GetPrefab(string name)
        {
            // Guard: ensure the asset bundle is available before attempting to load assets from it.
            if (Mod.Bundle == null)
            {
                DebugLogSystem.LogWarning("Mystery Meat attempted to load a prefab before the asset bundle was initialised.");
                return null;
            }

            // Guard: avoid attempting to load assets when the requested name is blank.
            if (string.IsNullOrWhiteSpace(name))
            {
                DebugLogSystem.LogWarning("Mystery Meat attempted to load a prefab with an empty name from the asset bundle.");
                return null;
            }

            GameObject prefab = Mod.Bundle.LoadAsset<GameObject>(name);

            // Guard: report missing prefabs so configuration issues can be diagnosed quickly.
            if (prefab == null)
            {
                DebugLogSystem.LogWarning($"Mystery Meat could not locate prefab '{name}' within the asset bundle.");
            }

            return prefab;
        }

        /// <summary>
        /// Configures a thin counter to supply a limited item, optionally wiring the held item position if available.
        /// </summary>
        /// <param name="counterPrefab">The counter prefab that will provide the item.</param>
        /// <param name="itemPrefab">The item prefab that should be spawned on the counter.</param>
        /// <param name="hasHeldItemPosition">Indicates whether the counter prefab exposes a held item position.</param>
        internal static void SetupThinCounterLimitedItem(GameObject counterPrefab, GameObject itemPrefab, bool hasHeldItemPosition)
        {
            Transform holdTransform = GameObjectUtils.GetChildObject(counterPrefab, "GameObject").transform;

            counterPrefab.TryAddComponent<HoldPointContainer>().HoldPoint = holdTransform;

            var sourceView = counterPrefab.TryAddComponent<LimitedItemSourceView>();

            // Only apply the held item position if the counter prefab defines one for held items.
            if (hasHeldItemPosition)
            {
                sourceView.HeldItemPosition = holdTransform;
            }

            ReflectionUtils.GetField<LimitedItemSourceView>("Items").SetValue(sourceView, new List<GameObject>()
            {
                GameObjectUtils.GetChildObject(counterPrefab, $"GameObject/{itemPrefab.name}")
            });
        }

        /// <summary>
        /// Configures a standard counter to supply a limited item via its hold point.
        /// </summary>
        /// <param name="counterPrefab">The counter prefab that will source the item.</param>
        /// <param name="itemPrefab">The limited item prefab to load into the counter.</param>
        internal static void SetupCounterLimitedItem(GameObject counterPrefab, GameObject itemPrefab)
        {
            Transform holdTransform = GameObjectUtils.GetChildObject(counterPrefab, "Block/HoldPoint").transform;

            counterPrefab.TryAddComponent<HoldPointContainer>().HoldPoint = holdTransform;

            var sourceView = counterPrefab.TryAddComponent<LimitedItemSourceView>();
            sourceView.HeldItemPosition = holdTransform;
            ReflectionUtils.GetField<LimitedItemSourceView>("Items").SetValue(sourceView, new List<GameObject>()
            {
                GameObjectUtils.GetChildObject(counterPrefab, $"Block/HoldPoint/{itemPrefab.name}")
            });
        }
    }
}
