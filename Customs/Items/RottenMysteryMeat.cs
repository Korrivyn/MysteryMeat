using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Items
{
    /// <summary>
    /// Defines the rotten meat variant used for visual storytelling when corpses decay.
    /// </summary>
    public class RottenMysteryMeat : CustomItem
    {
        public override string UniqueNameID => "RottenMysteryMeat";
        public override GameObject Prefab => Mod.Bundle.LoadAsset<GameObject>("Rotten Mystery Meat").AssignMaterialsByNames().AssignVFXByNames();
        public override ItemStorage ItemStorageFlags => ItemStorage.StackableFood;

        /// <summary>
        /// Emits diagnostics indicating that rotten meat currently only serves as a cosmetic or narrative prop.
        /// </summary>
        /// <param name="gameDataObject">The item definition being registered.</param>
        public override void OnRegister(Item gameDataObject)
        {
            base.OnRegister(gameDataObject);

            DebugLogSystem.LogVerbose("RottenMysteryMeat registered without active processes; serves as decay flavor content.");
        }
    }
}
