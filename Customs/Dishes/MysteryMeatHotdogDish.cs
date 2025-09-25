using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Customs.Processes;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Dishes
{
    /// <summary>
    /// Unlocks the hotdog follow-up dish that expands the menu with casings and minced meat workflows.
    /// </summary>
    public class MysteryMeatHotdogDish : CustomDish
    {
        public override string UniqueNameID => "MysteryMeatHotdogDish";
        public override DishType Type => DishType.Main;
        public override GameObject DisplayPrefab => (GDOUtils.GetExistingGDO(DishReferences.HotdogBase) as Dish).DisplayPrefab;
        public override GameObject IconPrefab => (GDOUtils.GetExistingGDO(DishReferences.HotdogBase) as Dish).IconPrefab;
        public override DishCustomerChange CustomerMultiplier => DishCustomerChange.SmallDecrease;
        public override CardType CardType => CardType.Default;
        public override Unlock.RewardLevel ExpReward => Unlock.RewardLevel.Medium;
        public override UnlockGroup UnlockGroup => UnlockGroup.Dish;
        public override bool DestroyAfterModUninstall => false;
        public override bool IsUnlockable => true;
        public override int Difficulty => 2;

        public override List<Unlock> HardcodedRequirements => new()
        {
            (Dish)GDOUtils.GetCustomGameDataObject<MysteryMeatBurgerDish>().GameDataObject
        };

        public override List<Dish.MenuItem> ResultingMenuItems => new()
        {
            new Dish.MenuItem
            {
                Item = (Item)GDOUtils.GetExistingGDO(ItemReferences.HotdogPlated),
                Phase = MenuPhase.Main,
                Weight = 1
            }
        };

        public override HashSet<Item> MinimumIngredients => new()
        {
            (Item)GDOUtils.GetCustomGameDataObject<MeatCleaver>().GameDataObject,
            (Item)GDOUtils.GetCustomGameDataObject<Casing>().GameDataObject,
            (Item)GDOUtils.GetExistingGDO(ItemReferences.HotdogBun)
        };

        public override HashSet<Process> RequiredProcesses => new()
        {
            (Process)GDOUtils.GetExistingGDO(ProcessReferences.Cook),
            GDOUtils.GetCastedGDO<Process, GrindMeat>()
        };

        public override Dictionary<Locale, string> Recipe => new()
        {
            { Locale.English, "Put 'fresh meat' in meat grinder to get minced meat. Combine with a hot dog casing, cook hot dog, and place in bun." }
        };

        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, new UnlockInfo
            {
                Name = "Mystery Meat Hot Dogs",
                Description = "Adds \"fresh meat\" hot dogs as a main",
                FlavourText = string.Empty
            })
        };

        /// <summary>
        /// Emits verbose diagnostics listing the new ingredient unlock requirements when registered.
        /// </summary>
        /// <param name="gameDataObject">The dish definition being registered.</param>
        public override void OnRegister(Dish gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int casingId = GDOUtils.GetCustomGameDataObject<Casing>().ID;

            DebugLogSystem.LogVerbose(
                $"MysteryMeatHotdogDish registered with casing dependency {casingId} and burger prerequisite unlock.");
        }
    }
}
