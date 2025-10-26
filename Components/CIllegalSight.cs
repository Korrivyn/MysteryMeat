using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Flags items or appliances that count as illegal sightings and optionally convert into another object at day start.
    /// </summary>
    public struct CIllegalSight : IModComponent, IAttachableProperty, IItemProperty, IApplianceProperty
    {
        /// <summary>
        /// Stores the game data ID that this entity should transform into when a new day begins.
        /// </summary>
        public int TurnIntoOnDayStart;
    }
}
