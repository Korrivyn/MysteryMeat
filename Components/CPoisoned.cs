using KitchenData;
using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Marks entities that have been poisoned so downstream systems can handle delayed kills or reactions.
    /// </summary>
    public struct CPoisoned : IModComponent, IItemProperty, IAttachableProperty
    {
    }
}
