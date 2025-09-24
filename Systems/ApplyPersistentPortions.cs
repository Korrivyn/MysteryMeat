using Kitchen;
using KitchenData;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    //[UpdateAfter(typeof(ObjectsSplittableView.UpdateView))]
    /// <summary>
    /// Restores splittable item portion counts from persisted data when a day starts.
    /// </summary>
    public class ApplyPersistentPortions : DaySystem, IModSystem
    {
        EntityQuery Query;

        /// <summary>
        /// Builds the query that locates splittable items with persisted portion data.
        /// </summary>
        protected override void Initialise()
        {
            base.Initialise();

            Query = GetEntityQuery(new QueryHelper()
                            .All(typeof(CPersistPortions), typeof(CItem), typeof(CSplittableItem), typeof(CLinkedView))
                            .None(typeof(CChangeItemType)));
        }

        /// <summary>
        /// Applies persisted portion values to the splittable items and removes the persistence marker.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _splittableItems = Query.ToEntityArray(Allocator.Temp);

            // Guard: exit when no splittable items require persistence application.
            if (_splittableItems.Length == 0)
            {
                return;
            }

            for (int i = _splittableItems.Length - 1; i >= 0; i--)
            {
                Entity splittableItem = _splittableItems[i];

                CPersistPortions cPersistPortions = GetComponent<CPersistPortions>(splittableItem);
                CSplittableItem cSplittableItem = GetComponent<CSplittableItem>(splittableItem);
                cSplittableItem.TotalCount = cPersistPortions.TotalCount;
                cSplittableItem.RemainingCount = cPersistPortions.RemainingCount;

                Set<CSplittableItem>(splittableItem, cSplittableItem);

                EntityManager.RemoveComponent<CPersistPortions>(splittableItem);
                DebugLogSystem.LogVerbose($"ApplyPersistentPortions restored {cPersistPortions.RemainingCount}/{cPersistPortions.TotalCount} portions for item {splittableItem.Index}.");
            }
        }
    }
}
