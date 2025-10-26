using Kitchen;
using Kitchen.Components;
using KitchenMods;
using KitchenMysteryMeat.Components;
using KitchenMysteryMeat.Enums;
using KitchenMysteryMeat.Systems.Logging;
using MessagePack;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenMysteryMeat.Views
{
    /// <summary>
    /// Presents suspicion and alert indicators above customers based on linked ECS data and preferences.
    /// </summary>
    public class SuspicionIndicatorView : UpdatableObjectView<SuspicionIndicatorView.ViewData>
    {
        public GameObject Canvas;
        public Image SuspicionIconFill;

        public GameObject SuspicionIconParent;
        public GameObject AlertIconParent;

        public AudioClip SuspicionClip;
        private SoundSource SuspicionSound;

        public AudioClip AlertClip;
        private SoundSource AlertSound;

        /// <summary>
        /// Initialises references to the indicator canvas and child icon transforms.
        /// </summary>
        private void Awake()
        {
            Canvas = transform.Find("Canvas").gameObject;
            SuspicionIconParent = Canvas.transform.Find("Suspicion").gameObject;
            AlertIconParent = Canvas.transform.Find("Alert").gameObject;
            SuspicionIconFill = SuspicionIconParent.transform.Find("Icon").GetComponent<Image>();
        }

        /// <summary>
        /// Refreshes the indicator state to mirror the supplied suspicion data and configured preferences.
        /// </summary>
        /// <param name="data">The latest suspicion indicator data from the linked entity.</param>
        protected override void UpdateData(SuspicionIndicatorView.ViewData data)
        {
            // Guard: ensure required UI references exist before applying updates.
            if (Canvas == null || SuspicionIconFill == null)
            {
                return;
            }

            // Guard: avoid preference lookups when the manager is unavailable, falling back to full volume.
            if (Mod.PrefManager == null)
            {
                if (!_missingPreferenceWarningLogged)
                {
                    DebugLogSystem.LogVerbose("Deferred suspicion indicator preference lookups because preferences are not initialised.");
                    _missingPreferenceWarningLogged = true;
                }
            }
            else
            {
                _missingPreferenceWarningLogged = false;
            }

            // Setup sus sound
            if (SuspicionClip != null)
            {
                // Guard: add the suspicion sound source when it has not yet been attached to the view.
                if (!SuspicionSound)
                {
                    SuspicionSound = gameObject.AddComponent<SoundSource>();
                    SuspicionSound.Configure(SoundCategory.Effects, SuspicionClip);
                }
            }

            if (AlertClip != null)
            {
                // Guard: add the alert sound source when it has not yet been attached to the view.
                if (!AlertSound)
                {
                    AlertSound = gameObject.AddComponent<SoundSource>();
                    AlertSound.Configure(SoundCategory.Effects, AlertClip);
                }
            }

            // Determine whether any indicator should be visible based on remaining time or explicit alert state.
            bool shouldShowIndicator = data.RemainingTime < data.TotalTime || data.IndicatorType == SuspicionIndicatorType.Alert;
            Canvas.SetActive(shouldShowIndicator);
            if (!shouldShowIndicator)
            {
                if (SuspicionSound)
                {
                    SuspicionSound.Stop();
                }

                if (AlertSound)
                {
                    AlertSound.Stop();
                }

                return;
            }

            bool isAlert = data.IndicatorType == SuspicionIndicatorType.Alert;
            bool isSuspicious = data.IndicatorType == SuspicionIndicatorType.Suspicious;

            if (!isAlert && AlertSound)
            {
                AlertSound.Stop();
            }

            if (!isSuspicious && SuspicionSound)
            {
                SuspicionSound.Stop();
            }

            if (isAlert)
            {
                // Show Alert Indicator
                AlertIconParent.SetActive(true);
                SuspicionIconParent.SetActive(false);

                if (AlertSound != null)
                {
                    if (!AlertSound.IsPlaying || AlertSound.TargetVolume == 0)
                    {
                        AlertSound.Play();
                    }

                    AlertSound.VolumeMultiplier = ResolveSuspicionVolumeMultiplier();
                }
            }
            else if (isSuspicious)
            {
                // Show Sus Indicator
                SuspicionIconParent.SetActive(true);
                AlertIconParent.SetActive(false);

                if (data.RemainingTime > 0.0f)
                {
                    // Fill amount starts from 0, then goes up
                    SuspicionIconFill.fillAmount = 1 - (data.RemainingTime / data.TotalTime);

                    if (SuspicionSound != null)
                    {
                        if (!SuspicionSound.IsPlaying || SuspicionSound.TargetVolume == 0)
                        {
                            SuspicionSound.Play();
                        }

                        SuspicionSound.VolumeMultiplier = SuspicionIconFill.fillAmount * ResolveSuspicionVolumeMultiplier();
                        SuspicionSound.Pitch = 0.5f + (1.5f * SuspicionIconFill.fillAmount);
                    }
                }
            }
            else if (AlertIconParent.activeSelf || SuspicionIconParent.activeSelf)
            {
                AlertIconParent.SetActive(false);
                SuspicionIconParent.SetActive(false);
            }
        }

        /// <summary>
        /// Keeps the indicator upright regardless of customer rotation to preserve readability.
        /// </summary>
        private void Update()
        {
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Drives suspicion indicator view updates from ECS component changes.
        /// </summary>
        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;

            /// <summary>
            /// Configures the entity query used to locate linked suspicion indicators.
            /// </summary>
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CSuspicionIndicator)));
            }

            /// <summary>
            /// Pushes suspicion indicator updates to the linked views each frame.
            /// </summary>
            protected override void OnUpdate()
            {
                using var views = query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using var suspicionIndicators = query.ToComponentDataArray<CSuspicionIndicator>(Allocator.Temp);

                for (int i = 0; i < views.Length; i++)
                {
                    CLinkedView view = views[i];
                    CSuspicionIndicator suspicionIndicator = suspicionIndicators[i];

                    SendUpdate(view, new ViewData
                    {
                        IndicatorType = suspicionIndicator.IndicatorType,
                        TotalTime = suspicionIndicator.TotalTime,
                        RemainingTime = suspicionIndicator.RemainingTime,
                    }, MessageType.SpecificViewUpdate);
                }
            }
        }

        [MessagePackObject(false)]
        public struct ViewData : ISpecificViewData, IViewData, IViewResponseData, IViewData.ICheckForChanges<ViewData>
        {
            [Key(0)] public SuspicionIndicatorType IndicatorType;
            [Key(1)] public float TotalTime;
            [Key(2)] public float RemainingTime;

            public IUpdatableObject GetRelevantSubview(IObjectView view) => view.GetSubView<SuspicionIndicatorView>();

            public bool IsChangedFrom(ViewData check) => check.IndicatorType != IndicatorType || check.RemainingTime != RemainingTime || check.TotalTime != TotalTime;
        }

        /// <summary>
        /// Resolves the configured suspicion volume multiplier while defaulting to full volume when preferences are unavailable.
        /// </summary>
        /// <returns>The effective suspicion volume multiplier.</returns>
        private static float ResolveSuspicionVolumeMultiplier()
        {
            if (Mod.PrefManager == null)
            {
                return 1.0f;
            }

            // Guard: clamp the resolved preference to the valid multiplier range before returning it.
            float resolvedVolume = Mathf.Clamp(Mod.PrefManager.Get<int>(Mod.SUSPICION_VOLUME_ID), 0, 100) / 100.0f;
            return resolvedVolume;
        }

        /// <summary>
        /// Tracks whether the missing preference manager warning has been logged to avoid spamming output.
        /// </summary>
        private static bool _missingPreferenceWarningLogged;
    }
}
