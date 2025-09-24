using Kitchen;
using KitchenData;
using KitchenLib.Utils;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using Unity.Collections;
using Unity.Entities;

namespace KitchenMysteryMeat.Systems
{
    /// <summary>
    /// Ensures trash bags expose item storage buffers so they can hold corpses safely.
    /// </summary>
    public class AttachTrashBagComponents : GameSystemBase, IModSystem
    {
        private EntityQuery TrashBags;
        /// <summary>
        /// Builds the query that locates trash bags lacking item storage components.
        /// </summary>
        protected override void Initialise()
        {
            TrashBags = GetEntityQuery(new QueryHelper()
                            .All(typeof(CTrashBag), typeof(CItem))
                            .None(typeof(CItemStorage)));
        }

        /// <summary>
        /// Adds storage capacity and buffers to trash bags so they can store corpses between uses.
        /// </summary>
        protected override void OnUpdate()
        {
            using NativeArray<Entity> _trashBags = TrashBags.ToEntityArray(Allocator.Temp);

            // Guard: exit when no trash bags require component attachment this frame.
            if (_trashBags.Length == 0)
            {
                return;
            }

            foreach (Entity trashBag in _trashBags)
            {
                Set<CItemStorage>(trashBag, new CItemStorage()
                {
                    Capacity = 1
                });
                EntityManager.AddBuffer<CItemStored>(trashBag);
                DebugLogSystem.LogVerbose($"AttachTrashBagComponents provisioned storage for trash bag {trashBag.Index}.");
            }
        }
    }
}
