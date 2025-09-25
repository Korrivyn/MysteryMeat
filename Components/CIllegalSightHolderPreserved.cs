using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Maintains a flag that notes when an illegal sight holder gained a preservation component for the next day.
    /// </summary>
    public struct CIllegalSightHolderPreserved : IModComponent
    {
        /// <summary>
        /// Indicates whether the preservation component has already been attached to prevent duplicate work.
        /// </summary>
        public bool AddedPreserver;
    }
}
