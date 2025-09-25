using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Tags an empty special sauce bottle so refill systems can restore it to the configured full variant.
    /// </summary>
    public struct CEmptyBottle : IModComponent, IItemProperty, IAttachableProperty
    {
        /// <summary>
        /// Identifies the item definition for the refilled bottle that should replace this empty container.
        /// </summary>
        public int FullBottleID;
    }
}
