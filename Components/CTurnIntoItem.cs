using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Directs systems to replace an item with another item definition when specific triggers fire.
    /// </summary>
    public struct CTurnIntoItem : IModComponent, IItemProperty, IAttachableProperty
    {
        /// <summary>
        /// Stores the ID for the item that should replace the current entity once the conversion happens.
        /// </summary>
        public int NewID;
    }
}
