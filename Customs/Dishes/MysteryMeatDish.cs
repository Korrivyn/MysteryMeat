using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KitchenMysteryMeat.Systems.Logging;

namespace KitchenMysteryMeat.Customs.Dishes
{
    public class MysteryMeatDish : CustomDish
    {
        public override string UniqueNameID => "Mystery Meat Recipe Only";
        public override GameObject DisplayPrefab => Mod.Bundle.LoadAsset<GameObject>("Meat Cleaver");
        public override GameObject IconPrefab => DisplayPrefab;
        public override UnlockGroup UnlockGroup => UnlockGroup.Dish;
        public override CardType CardType => CardType.Default;
        public override bool IsUnlockable => false;

        public override DishType Type => DishType.Base;

        public override Dictionary<Locale, string> Recipe => new()
        {
            { Locale.English, "Grab meat cleaver and interact with customers to dispatch them. Grab corpse off of floor and portion to get 'fresh meat' for recipes. Make sure no customers see blood or a corpse or else they may run away." }
        };
        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateUnlockInfo("Mystery Meat", "Redrum", "How'd you get here?"))
        };

        /// <summary>
        /// Hides the info panel for the hidden base dish when registered.
        /// </summary>
        public override void OnRegister(Dish gdo)
        {
            gdo.HideInfoPanel = true;
            DebugLogSystem.LogVerbose("Registration hid the info panel for the hidden dish.");
        }
    }
}
