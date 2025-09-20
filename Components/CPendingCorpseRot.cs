using Unity.Entities;

namespace KitchenMysteryMeat.Components
{
    public struct CPendingCorpseRot : IComponentData
    {
        public int TargetItemID;
        public int TargetApplianceID;
        public float Duration;
        public float Elapsed;
        public bool PreservePortions;
    }
}
