using Kitchen;
using KitchenMods;
using KitchenMysteryMeat.Components;
using MessagePack;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenMysteryMeat.Views
{
    public class CorpseRotView : UpdatableObjectView<CorpseRotView.ViewData>
    {
        [SerializeField]
        private GameObject RottedPrefab;

        private RendererState[] RendererStates = System.Array.Empty<RendererState>();

        internal void Configure(GameObject rottedPrefab)
        {
            RottedPrefab = rottedPrefab;
        }

        private void Awake()
        {
            PrepareRendererStates();
            UpdateData(new ViewData { Progress = 0f });
        }

        private void PrepareRendererStates()
        {
            Renderer[] freshRenderers = GetComponentsInChildren<Renderer>(true);
            if (freshRenderers == null || freshRenderers.Length == 0)
            {
                RendererStates = System.Array.Empty<RendererState>();
                return;
            }

            Dictionary<string, Material[]> targetLookup = BuildTargetLookup();
            RendererStates = new RendererState[freshRenderers.Length];

            for (int i = 0; i < freshRenderers.Length; i++)
            {
                Renderer renderer = freshRenderers[i];
                Material[] shared = renderer.sharedMaterials;
                Material[] baseMaterials = new Material[shared.Length];
                Material[] workingMaterials = new Material[shared.Length];

                for (int m = 0; m < shared.Length; m++)
                {
                    baseMaterials[m] = new Material(shared[m]);
                    workingMaterials[m] = new Material(shared[m]);
                }

                renderer.materials = workingMaterials;

                string path = GetRelativePath(renderer.transform, transform);
                Material[] targetMaterials = null;

                if (!string.IsNullOrEmpty(path))
                {
                    targetLookup.TryGetValue(path, out targetMaterials);
                }

                if (targetMaterials == null || targetMaterials.Length == 0)
                {
                    targetMaterials = new Material[shared.Length];
                    for (int m = 0; m < shared.Length; m++)
                    {
                        targetMaterials[m] = new Material(shared[m]);
                    }
                }
                else
                {
                    if (targetMaterials.Length != shared.Length)
                    {
                        Material[] adjusted = new Material[shared.Length];
                        for (int m = 0; m < shared.Length; m++)
                        {
                            adjusted[m] = m < targetMaterials.Length
                                ? new Material(targetMaterials[m])
                                : new Material(shared[m]);
                        }

                        targetMaterials = adjusted;
                    }
                    else
                    {
                        Material[] cloned = new Material[targetMaterials.Length];
                        for (int m = 0; m < targetMaterials.Length; m++)
                        {
                            cloned[m] = new Material(targetMaterials[m]);
                        }

                        targetMaterials = cloned;
                    }
                }

                RendererStates[i] = new RendererState
                {
                    BaseMaterials = baseMaterials,
                    WorkingMaterials = workingMaterials,
                    TargetMaterials = targetMaterials
                };
            }
        }

        private Dictionary<string, Material[]> BuildTargetLookup()
        {
            Dictionary<string, Material[]> lookup = new Dictionary<string, Material[]>(System.StringComparer.Ordinal);
            if (RottedPrefab == null)
            {
                return lookup;
            }

            Renderer[] rottedRenderers = RottedPrefab.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rottedRenderers.Length; i++)
            {
                Renderer renderer = rottedRenderers[i];
                string path = GetRelativePath(renderer.transform, RottedPrefab.transform);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                lookup[path] = renderer.sharedMaterials;
            }

            return lookup;
        }

        private static string GetRelativePath(Transform target, Transform root)
        {
            if (target == null || root == null)
            {
                return string.Empty;
            }

            List<string> segments = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                segments.Insert(0, current.name);
                current = current.parent;
            }

            if (current == null || segments.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("/", segments);
        }

        protected override void UpdateData(ViewData data)
        {
            float progress = Mathf.Clamp01(data.Progress);

            for (int i = 0; i < RendererStates.Length; i++)
            {
                RendererState state = RendererStates[i];
                if (state.WorkingMaterials == null)
                {
                    continue;
                }

                for (int m = 0; m < state.WorkingMaterials.Length; m++)
                {
                    Material working = state.WorkingMaterials[m];
                    Material start = state.BaseMaterials.Length > m ? state.BaseMaterials[m] : null;
                    Material target = state.TargetMaterials.Length > m ? state.TargetMaterials[m] : null;

                    if (working == null || start == null)
                    {
                        continue;
                    }

                    if (target == null)
                    {
                        target = start;
                    }

                    working.Lerp(start, target, progress);
                }
            }
        }

        private struct RendererState
        {
            public Material[] BaseMaterials;
            public Material[] WorkingMaterials;
            public Material[] TargetMaterials;
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public float Progress;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<CorpseRotView>();

            public bool IsChangedFrom(ViewData check) => !Mathf.Approximately(check.Progress, Progress);
        }

        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery PendingRots;

            protected override void Initialise()
            {
                base.Initialise();
                PendingRots = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CPendingCorpseRot)));
            }

            protected override void OnUpdate()
            {
                using NativeArray<Entity> entities = PendingRots.ToEntityArray(Allocator.Temp);
                using NativeArray<CLinkedView> views = PendingRots.ToComponentDataArray<CLinkedView>(Allocator.Temp);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!Require(entity, out CPendingCorpseRot pending))
                    {
                        continue;
                    }

                    float duration = pending.Duration <= 0.001f ? 0.001f : pending.Duration;
                    float progress = Mathf.Clamp01(pending.Elapsed / duration);

                    SendUpdate(views[i], new ViewData
                    {
                        Progress = progress
                    }, MessageType.SpecificViewUpdate);
                }
            }
        }
    }
}
