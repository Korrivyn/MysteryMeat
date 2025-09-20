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
using KitchenMysteryMeat.MonoBehaviours;

namespace KitchenMysteryMeat.Views
{
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

        private void Awake()
        {
            Canvas = transform.Find("Canvas").gameObject;
            SuspicionIconParent = Canvas.transform.Find("Suspicion").gameObject;
            AlertIconParent = Canvas.transform.Find("Alert").gameObject;
            SuspicionIconFill = SuspicionIconParent.transform.Find("Icon").GetComponent<Image>();
        }

        protected override void UpdateData(SuspicionIndicatorView.ViewData data)
        {
            if (Canvas == null || SuspicionIconFill == null)
                return;

            // Setup sus sound
            if (SuspicionClip != null)
            {
                if (!SuspicionSound)
                {
                    SuspicionSound = base.gameObject.AddComponent<SoundSource>();
                    SuspicionSound.Configure(SoundCategory.Effects, SuspicionClip);
                }
            }

            if (AlertClip != null)
            {
                if (!AlertSound)
                {
                    AlertSound = base.gameObject.AddComponent<SoundSource>();
                    AlertSound.Configure(SoundCategory.Effects, AlertClip);
                }
            }

            bool shouldShowIndicator = data.RemainingTime < data.TotalTime || data.IndicatorType == SuspicionIndicatorType.Alert;
            Canvas.SetActive(shouldShowIndicator);
            if (!shouldShowIndicator)
            {
                if (SuspicionSound)
                    SuspicionSound.Stop();
                if (AlertSound)
                    AlertSound.Stop();
                return;
            }

            bool isAlert = data.IndicatorType == SuspicionIndicatorType.Alert;
            bool isSuspicious = data.IndicatorType == SuspicionIndicatorType.Suspicious;

            if (!isAlert && AlertSound)
                AlertSound.Stop();

            if (!isSuspicious && SuspicionSound)
                SuspicionSound.Stop();


            if (isAlert)
            {
                // Show Alert Indicator
                AlertIconParent.SetActive(true);
                SuspicionIconParent.SetActive(false);

                if (AlertSound)
                {
                    if (!AlertSound.IsPlaying || AlertSound.TargetVolume == 0)
                        AlertSound.Play();

                    AlertSound.VolumeMultiplier = Mod.PrefManager.Get<int>(Mod.SUSPICION_VOLUME_ID) / 100.0f;
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

                    if (!SuspicionSound.IsPlaying || SuspicionSound.TargetVolume == 0)
                        SuspicionSound.Play();
                    SuspicionSound.VolumeMultiplier = SuspicionIconFill.fillAmount * (Mod.PrefManager.Get<int>(Mod.SUSPICION_VOLUME_ID) / 100.0f);
                    SuspicionSound.Pitch = 0.5f + (1.5f * SuspicionIconFill.fillAmount);
                }
            }
            else if (AlertIconParent.activeSelf || SuspicionIconParent.activeSelf)
            {
                AlertIconParent.SetActive(false);
                SuspicionIconParent.SetActive(false);
            }
        }

        private void Update()
        {
            transform.rotation = Quaternion.identity;
        }

        public class UpdateView : IncrementalViewSystemBase<ViewData>, IModSystem
        {
            private EntityQuery query;
            protected override void Initialise()
            {
                base.Initialise();
                query = GetEntityQuery(new QueryHelper().All(typeof(CLinkedView), typeof(CSuspicionIndicator)));
            }

            protected override void OnUpdate()
            {
                using var views = query.ToComponentDataArray<CLinkedView>(Allocator.Temp);
                using var suspicionIndicators = query.ToComponentDataArray<CSuspicionIndicator>(Allocator.Temp);

                for (var i = 0; i < views.Length; i++)
                {
                    var view = views[i];
                    var suspicionIndicator = suspicionIndicators[i];

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
    }
}
