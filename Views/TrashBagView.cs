using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Systems.Logging;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Views
{
    /// <summary>
    /// Synchronises the trash bag visuals with stored corpse data to represent remaining portions.
    /// </summary>
    public class TrashBagView : UpdatableObjectView<TrashBagView.ViewData>
    {
        public Transform TrashBag;
        public Transform CorpsesParent;

        /// <summary>
        /// Captures transform references for the trash bag visuals and corpse container.
        /// </summary>
        private void Awake()
        {
            TrashBag = transform.Find("Trash Bag");
            CorpsesParent = transform.Find("Corpses");
            TrashBag.gameObject.SetActive(true);
            CorpsesParent.gameObject.SetActive(false);
        }

        /// <summary>
        /// Toggles corpse meshes based on the supplied view data and tracks invalid state for debugging.
        /// </summary>
        protected override void UpdateData(TrashBagView.ViewData data)
        {
            // Guard: ensure required transforms are available before applying updates.
            if (TrashBag == null || CorpsesParent == null)
            {
                DebugLogSystem.LogWarning("Trash bag view attempted to update without configured transforms.");
                return;
            }

            TrashBag.gameObject.SetActive(!data.ContainsCorpse);
            CorpsesParent.gameObject.SetActive(data.ContainsCorpse);

            if (data.ContainsCorpse)
            {
                for (int i = 0; i < CorpsesParent.childCount; i++)
                {
                    Transform child = CorpsesParent.GetChild(i);
                    bool shouldActivate = (data.TotalPortions - data.RemainingPortions) == i;
                    child.gameObject.SetActive(shouldActivate);
                }
            }
        }

        /// <summary>
        /// System responsible for pushing trash bag view updates from ECS data.
        /// </summary>
        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;
            /// <summary>
            /// Builds the query that locates trash bags with linked views and stored items.
            /// </summary>
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CTrashBag), typeof(CItemStored)));
            }

            /// <summary>
            /// Pushes updated corpse counts to the linked trash bag views.
            /// </summary>
            protected override void OnUpdate()
            {
                using var entities = query.ToEntityArray(Allocator.Temp);
                using var views = query.ToComponentDataArray<CLinkedView>(Allocator.Temp);

                for (var i = 0; i < views.Length; i++)
                {
                    var view = views[i];
                    var itemStored = GetBuffer<CItemStored>(entities[i]);
                    // Since I know there will only ever be 1, just get first index of stored items
                    if (itemStored.Length > 0 && itemStored[0].StoredItem != default && Require<CSplittableItem>(itemStored[0].StoredItem, out var cSplittableItem))
                    {
                        SendUpdate(view, new ViewData
                        {
                            ContainsCorpse = true,
                            TotalPortions = cSplittableItem.TotalCount,
                            RemainingPortions = cSplittableItem.RemainingCount,
                        }, MessageType.SpecificViewUpdate);
                        DebugLogSystem.LogVerbose($"Pushed corpse counts {cSplittableItem.RemainingCount}/{cSplittableItem.TotalCount} for trash bag entity {entities[i].Index}.");
                    }
                    else
                    {
                        SendUpdate(view, new ViewData
                        {
                            ContainsCorpse = false,
                        }, MessageType.SpecificViewUpdate);
                        if (itemStored.Length == 0)
                        {
                            DebugLogSystem.LogVerbose($"Cleared corpse visuals because trash bag {entities[i].Index} has no stored items.");
                        }
                    }
                }
            }
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public bool ContainsCorpse;
            [Key(1)] public int TotalPortions;
            [Key(2)] public int RemainingPortions;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<TrashBagView>();

            public bool IsChangedFrom(ViewData check) => check.TotalPortions != TotalPortions || check.RemainingPortions != RemainingPortions || check.ContainsCorpse != ContainsCorpse;
        }
    }
}
