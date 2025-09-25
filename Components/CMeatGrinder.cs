using KitchenData;
using KitchenMods;
using UnityEngine;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Exposes the offsets and process ID required for appliances that animate and output ground meat.
    /// </summary>
    public struct CMeatGrinder : IModComponent, IAttachableProperty, IApplianceProperty
    {
        /// <summary>
        /// Captures the world-space position where ingredients enter the grinder.
        /// </summary>
        public Vector3 GrinderInputPosition;

        /// <summary>
        /// Captures the world-space position where processed meat exits the grinder.
        /// </summary>
        public Vector3 GrinderOutputPosition;

        /// <summary>
        /// Links to the grind process that should be triggered when items enter the appliance.
        /// </summary>
        public int GrindProcess;
    }
}
