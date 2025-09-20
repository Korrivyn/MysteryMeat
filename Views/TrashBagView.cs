using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
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
    public class TrashBagView : UpdatableObjectView<TrashBagView.ViewData>
    {
        public Transform TrashBag;
        public Transform CorpsesParent;
        private void Awake()
        {
            TrashBag = transform.Find("Trash Bag");
            CorpsesParent = transform.Find("Corpses");
            TrashBag.gameObject.SetActive(true);
            CorpsesParent.gameObject.SetActive(false);
        }

        protected override void UpdateData(TrashBagView.ViewData data)
        {
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

        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CTrashBag), typeof(CItemStored)));
            }

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
                    }   
                    else
                    {
                        SendUpdate(view, new ViewData
                        {
                            ContainsCorpse = false,
                        }, MessageType.SpecificViewUpdate);
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
