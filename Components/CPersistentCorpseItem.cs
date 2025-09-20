using KitchenMods;

namespace KitchenMysteryMeat.Components
{
    /// <summary>
    /// Tags corpse items that received temporary overnight preservation from the
    /// Persistent Corpses status so we can clear the added components cleanly when
    /// the status is removed.
    /// </summary>
    public struct CPersistentCorpseItem : IModComponent
    {
    }
}
