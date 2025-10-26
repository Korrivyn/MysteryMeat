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
    /// Unlocks the core mystery meat burger offering that introduces corpse harvesting and grinder gameplay.
    /// </summary>
    public class MysteryMeatBurgerDish : CustomDish
    {
        public override string UniqueNameID => "MysteryMeatBurgerDish";
        public override Unlock.RewardLevel ExpReward => Unlock.RewardLevel.Medium;
        public override bool IsUnlockable => true;
        public override UnlockGroup UnlockGroup => UnlockGroup.Dish;
        public override CardType CardType => CardType.Default;
        public override DishCustomerChange CustomerMultiplier => DishCustomerChange.SmallDecrease;
        public override DishType Type => DishType.Base;
        public override int Difficulty => 3;
        public override List<string> StartingNameSet => new()
        {
            "We Won't Kill You",
            "Fresh Never Frozen"
        };

        public override HashSet<Item> MinimumIngredients => new()
        {
            (Item)GDOUtils.GetCustomGameDataObject<MeatCleaver>().GameDataObject,
            (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerBun),
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Water),
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Plate)
        };

        public override HashSet<Process> RequiredProcesses => new()
        {
            (Process)GDOUtils.GetExistingGDO(ProcessReferences.Cook),
            GDOUtils.GetCastedGDO<Process, GrindMeat>()
        };

        public override GameObject IconPrefab => Mod.Bundle.LoadAsset<GameObject>("Mystery Meat - Icon").AssignMaterialsByNames();
        public override GameObject DisplayPrefab => (GDOUtils.GetExistingGDO(DishReferences.BurgerBase) as Dish).DisplayPrefab;

        public override List<Dish.MenuItem> ResultingMenuItems => new()
        {
            new Dish.MenuItem
            {
                Item = (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerPlated),
                Phase = MenuPhase.Main,
                Weight = 1,
                DynamicMenuType = DynamicMenuType.Static,
                DynamicMenuIngredient = null
            }
        };

        public override bool IsAvailableAsLobbyOption => true;
        public override HashSet<Item> BlockProviders => new()
        {
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Meat),
            (Item)GDOUtils.GetExistingGDO(ItemReferences.BurgerPattyRaw)
        };

        public override Dictionary<Locale, string> Recipe => new()
        {
            { Locale.English, "Put 'fresh meat' in meat grinder to get minced meat. Knead to form patty. Cook burger patty and add bun." }
        };

        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, new UnlockInfo
            {
                Name = "Mystery Meat Burgers",
                Description = "Adds \"fresh meat\" burgers as a main",
                FlavourText = string.Empty
            })
        };

        public override List<Dish> AlsoAddRecipes => new()
        {
            (Dish)GDOUtils.GetCustomGameDataObject<MysteryMeatDish>().GameDataObject
        };

        /// <summary>
        /// Emits verbose diagnostics capturing the grinder dependency when the dish is registered.
        /// </summary>
        /// <param name="gameDataObject">The dish definition being registered.</param>
        public override void OnRegister(Dish gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int grindProcessId = GDOUtils.GetCustomGameDataObject<GrindMeat>().ID;

            DebugLogSystem.LogVerbose(
                $"MysteryMeatBurgerDish registered with grind process {grindProcessId} and blocked vanilla meat providers.");
        }
    }
}
