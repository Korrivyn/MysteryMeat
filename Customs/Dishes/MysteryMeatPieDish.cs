using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;
using UnityEngine;

namespace KitchenMysteryMeat.Customs.Dishes
{
    /// <summary>
    /// Unlocks the pie variant that extends mystery meat into oven-baked dishes with pastry preparation.
    /// </summary>
    public class MysteryMeatPieDish : CustomDish
    {
        public override string UniqueNameID => "MysteryMeatPieDish";
        public override DishType Type => DishType.Main;
        public override GameObject DisplayPrefab => (GDOUtils.GetExistingGDO(DishReferences.PieBase) as Dish).DisplayPrefab;
        public override GameObject IconPrefab => (GDOUtils.GetExistingGDO(DishReferences.PieBase) as Dish).IconPrefab;
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
                Item = (Item)GDOUtils.GetExistingGDO(ItemReferences.PiePlated),
                Phase = MenuPhase.Main,
                Weight = 1
            }
        };

        public override HashSet<Dish.IngredientUnlock> IngredientsUnlocks => new()
        {
            new Dish.IngredientUnlock
            {
                Ingredient = (Item)GDOUtils.GetExistingGDO(ItemReferences.PieMeatCooked),
                MenuItem = (ItemGroup)GDOUtils.GetExistingGDO(ItemReferences.PiePlated)
            }
        };

        public override HashSet<Item> MinimumIngredients => new()
        {
            (Item)GDOUtils.GetCustomGameDataObject<MeatCleaver>().GameDataObject,
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Water),
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Flour),
            (Item)GDOUtils.GetExistingGDO(ItemReferences.Plate)
        };

        public override HashSet<Process> RequiredProcesses => new()
        {
            (Process)GDOUtils.GetExistingGDO(ProcessReferences.RequireOven),
            (Process)GDOUtils.GetExistingGDO(ProcessReferences.Knead)
        };

        public override Dictionary<Locale, string> Recipe => new()
        {
            { Locale.English, "Knead flour (or add water) to create dough, then knead into pie crust. Add 'fresh meat' and cook." }
        };

        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, new UnlockInfo
            {
                Name = "Mystery Meat Pies",
                Description = "Adds \"fresh meat\" pies as a main",
                FlavourText = string.Empty
            })
        };

        /// <summary>
        /// Logs the special oven requirement that differentiates the pie unlock.
        /// </summary>
        /// <param name="gameDataObject">The dish definition being registered.</param>
        public override void OnRegister(Dish gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int ovenRequirementId = GDOUtils.GetExistingGDO(ProcessReferences.RequireOven).ID;

            DebugLogSystem.LogVerbose(
                $"MysteryMeatPieDish registered with oven requirement {ovenRequirementId} and pastry ingredient unlock.");
        }
    }
}
