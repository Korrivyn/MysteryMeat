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
    /// Drives the limited-use bottle mesh materials to reflect remaining charges.
    /// </summary>
    public class LimitedUseBottleView : UpdatableObjectView<LimitedUseBottleView.ViewData>
    {
        public GameObject Mesh;
        public Material BottleMaterial;
        public Material LiquidMaterial;

        /// <summary>
        /// Resolves the mesh used for material swapping when the view awakens.
        /// </summary>
        private void Awake()
        {
            Mesh = transform.Find("LimitedUseBottle").gameObject;
        }

        /// <summary>
        /// Updates the bottle materials to reflect the supplied fill amount.
        /// </summary>
        protected override void UpdateData(LimitedUseBottleView.ViewData data)
        {
            if (!Mesh || !BottleMaterial || !LiquidMaterial)
            {
                DebugLogSystem.LogWarning("Limited-use bottle view is missing mesh or material references and cannot update visuals.");
                return;
            }

            if (data.Equals(default(ViewData)))
            {
                DebugLogSystem.LogVerbose("Received default view data; skipping limited-use bottle update.");
                return;
            }

            MeshRenderer renderer = Mesh.GetComponent<MeshRenderer>();
            Material[] newMats = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material desiredMaterial = BottleMaterial;
                if (i < data.FillAmount)
                {
                    desiredMaterial = LiquidMaterial;
                }
                newMats[i] = desiredMaterial;
            }
            Mesh.GetComponent<MeshRenderer>().materials = newMats;
        }

        /// <summary>
        /// System responsible for keeping limited-use bottle visuals in sync with ECS data.
        /// </summary>
        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;
            /// <summary>
            /// Builds the query that locates limited-use bottles with linked views.
            /// </summary>
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CLimitedUseBottle)));
            }

            /// <summary>
            /// Sends fill amount and limit information to each bottle view.
            /// </summary>
            protected override void OnUpdate()
            {
                using var views = query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using var limitedUseBottleComponents = query.ToComponentDataArray<CLimitedUseBottle>(Allocator.Temp);

                for (var i = 0; i < views.Length; i++)
                {
                    var view = views[i];
                    var limitedUseBottle = limitedUseBottleComponents[i];

                    SendUpdate(view, new ViewData
                    {
                        Limit = limitedUseBottle.Limit,
                        FillAmount = limitedUseBottle.FillAmount
                    }, MessageType.SpecificViewUpdate);
                    DebugLogSystem.LogVerbose($"Queued fill amount {limitedUseBottle.FillAmount}/{limitedUseBottle.Limit} for limited-use bottle view entity {view.Entity.Index}.");
                }
            }
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public int Limit;
            [Key(1)] public int FillAmount;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<LimitedUseBottleView>();

            public bool IsChangedFrom(ViewData check) => check.Limit != Limit || check.FillAmount != FillAmount;
        }
    }
}
