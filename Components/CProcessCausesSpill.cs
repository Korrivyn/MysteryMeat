using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Describes spill behaviour that is triggered when a specific process runs on an item or appliance.
    /// </summary>
    public struct CProcessCausesSpill : IModComponent, IItemProperty, IAttachableProperty
    {
        /// <summary>
        /// Identifies the process that should emit spill messes when executed.
        /// </summary>
        public int Process;

        /// <summary>
        /// Points to the mess ID that should be spawned as part of the spill.
        /// </summary>
        public int ID;

        /// <summary>
        /// Indicates how quickly the spill should accumulate while the process runs.
        /// </summary>
        public float Rate;

        /// <summary>
        /// Determines whether the generated mess should overwrite existing messes at the location.
        /// </summary>
        public bool OverwriteOtherMesses;
    }
}
