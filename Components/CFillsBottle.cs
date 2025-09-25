using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Marks appliances that can refill empty bottles with the specified filled bottle item.
    /// </summary>
    public struct CFillsBottle : IModComponent, IApplianceProperty, IAttachableProperty
    {
        /// <summary>
        /// Identifies the bottle item that is dispensed after a successful refill interaction.
        /// </summary>
        public int BottleID;
    }
}
