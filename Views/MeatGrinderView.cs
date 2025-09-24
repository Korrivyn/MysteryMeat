using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Enums;
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
using UnityEngine.UI;
using Kitchen.Components;
using KitchenMysteryMeat.Systems.Logging;


namespace KitchenMysteryMeat.Views
{
    /// <summary>
    /// Animates the grinder hold point to mirror grindable input presence and process progress.
    /// </summary>
    public class MeatGrinderView : UpdatableObjectView<MeatGrinderView.ViewData>
    {
        public GameObject HoldPoint;

        /// <summary>
        /// Resolves the hold point transform used for visual adjustments.
        /// </summary>
        private void Awake()
        {
            HoldPoint = transform.Find("GameObject").gameObject;
        }

        /// <summary>
        /// Updates the hold point location and scale according to the supplied view data.
        /// </summary>
        protected override void UpdateData(MeatGrinderView.ViewData data)
        {
            if (HoldPoint == null)
            {
                DebugLogSystem.LogWarning("MeatGrinderView missing hold point transform; skipping update.");
                return;
            }

            if (data.HasGrindableItem)
            {
                HoldPoint.transform.localPosition = data.GrinderInputPosition;
            }
            else
            {
                HoldPoint.transform.localPosition = data.GrinderOutputPosition;
            }
            float inverseProgress = 1 - data.ProcessProgress;
            HoldPoint.transform.localScale = new Vector3(inverseProgress, inverseProgress, inverseProgress);
        }

        /// <summary>
        /// System responsible for driving meat grinder view updates based on ECS state.
        /// </summary>
        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;
            /// <summary>
            /// Builds the query that locates meat grinders with linked views and process data.
            /// </summary>
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CMeatGrinder), typeof(CApplyingProcess), typeof(CItemHolder)));
            }

            /// <summary>
            /// Sends grinder state updates to the associated views.
            /// </summary>
            protected override void OnUpdate()
            {
                using var views = query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using var meatGrinders = query.ToComponentDataArray<CMeatGrinder>(Allocator.Temp);
                using var applyingProcessComponents = query.ToComponentDataArray<CApplyingProcess>(Allocator.Temp);
                using var itemHolders = query.ToComponentDataArray<CItemHolder>(Allocator.Temp);

                for (var i = 0; i < views.Length; i++)
                {
                    var view = views[i];
                    var meatGrinder = meatGrinders[i];
                    var applyingProcess = applyingProcessComponents[i];
                    var hasGrindable = Has<CGrindable>(itemHolders[i].HeldItem);

                    SendUpdate(view, new ViewData
                    {
                        HasGrindableItem = hasGrindable,
                        ProcessProgress = applyingProcess.Progress,
                        GrinderInputPosition = meatGrinder.GrinderInputPosition,
                        GrinderOutputPosition = meatGrinder.GrinderOutputPosition,
                    }, MessageType.SpecificViewUpdate);
                    DebugLogSystem.LogVerbose($"MeatGrinderView.UpdateView queued progress {applyingProcess.Progress:P0} for grinder entity {view.Entity.Index}.");
                }
            }
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public bool HasGrindableItem;
            [Key(1)] public float ProcessProgress;
            [Key(2)] public Vector3 GrinderInputPosition;
            [Key(3)] public Vector3 GrinderOutputPosition;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<MeatGrinderView>();

            public bool IsChangedFrom(ViewData check) => check.HasGrindableItem != HasGrindableItem || check.ProcessProgress != ProcessProgress;
        }
    }
}
