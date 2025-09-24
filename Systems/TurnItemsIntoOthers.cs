using Kitchen;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Converts tagged items into their configured replacements once the required data is present.
    /// </summary>
    public class TurnItemsIntoOthers : GenericSystemBase, IModSystem
    {
        EntityQuery Items;

        /// <summary>
        /// Builds the query that locates items awaiting transformation.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            Items = GetEntityQuery(typeof(CItem), typeof(CTurnIntoItem));

        }

        /// <summary>
        /// Applies transformation requests by swapping item types and clearing the marker component.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _items = Items.ToEntityArray(Allocator.Temp);

            // Guard: exit early when no items require transformation this frame.
            if (_items.Length == 0)
            {
                return;
            }

            foreach (Entity item in _items)
            {
                CTurnIntoItem cTurnIntoItem = GetComponent<CTurnIntoItem>(item);

                // Guard: skip when the transformation target has not been configured.
                if (cTurnIntoItem.NewID == 0)
                {
                    DebugLogSystem.LogWarning($"TurnItemsIntoOthers found item {item.Index} with an unset NewID; transformation skipped.");
                    continue;
                }
                EntityManager.AddComponentData<CChangeItemType>(item, new CChangeItemType()
                {
                    NewID = cTurnIntoItem.NewID,
                });
                EntityManager.RemoveComponent<CTurnIntoItem>(item);
                DebugLogSystem.LogVerbose($"TurnItemsIntoOthers changed item {item.Index} to new item ID {cTurnIntoItem.NewID}.");
            }
        }
    }
}
