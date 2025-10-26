using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Restores empty special sauce bottles to their configured replacement items so tables remain stocked.
    /// </summary>
    public class ReplaceEmptySpecialSauce : GenericSystemBase, IModSystem
    {
        EntityQuery Items;

        /// <summary>
        /// Builds the query used to locate limited-use bottles that may require replacement.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            Items = GetEntityQuery(typeof(CItem), typeof(CLimitedUseBottle));

        }

        /// <summary>
        /// Swaps empty sauce bottles for their configured replacements while reporting any unusual fill states.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _items = Items.ToEntityArray(Allocator.Temp);

            // Guard: exit early when no bottles require evaluation this frame.
            if (_items.Length == 0)
            {
                return;
            }

            foreach (Entity item in _items)
            {
                CLimitedUseBottle cLimitedUseBottle = GetComponent<CLimitedUseBottle>(item);

                // Guard: emit a warning when the fill amount dips below zero, indicating unexpected underflow.
                if (cLimitedUseBottle.FillAmount < 0)
                {
                    DebugLogSystem.LogWarning("Detected a negative FillAmount on a sauce bottle; resetting to zero before replacement.");
                    cLimitedUseBottle.FillAmount = 0;
                    EntityManager.SetComponentData(item, cLimitedUseBottle);
                }

                // Guard: skip replacement when the bottle still contains charges.
                if (cLimitedUseBottle.FillAmount <= 0)
                {
                    DebugLogSystem.LogVerbose($"Converting bottle entity {item.Index} to empty ID {cLimitedUseBottle.EmptyBottleID}.");
                    EntityManager.AddComponentData<CChangeItemType>(item, new CChangeItemType()
                    {
                        NewID = cLimitedUseBottle.EmptyBottleID,
                        CollapseComponents = true
                    });

                    EntityManager.RemoveComponent<CLimitedUseBottle>(item);
                }
            }
        }
    }
}
