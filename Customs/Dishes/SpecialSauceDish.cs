using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Items;
using KitchenMysteryMeat.Systems.Logging;

namespace KitchenMysteryMeat.Customs.Dishes
{
    /// <summary>
    /// Unlocks the special sauce request system that layers blood refills onto plated meals.
    /// </summary>
    public class SpecialSauceDish : CustomDish
    {
        public override string UniqueNameID => "SpecialSauceDish";
        public override DishType Type => DishType.Extra;
        public override DishCustomerChange CustomerMultiplier => DishCustomerChange.SmallDecrease;
        public override CardType CardType => CardType.Default;
        public override Unlock.RewardLevel ExpReward => Unlock.RewardLevel.Medium;
        public override UnlockGroup UnlockGroup => UnlockGroup.Dish;
        public override bool IsSpecificFranchiseTier => false;
        public override bool DestroyAfterModUninstall => false;
        public override bool IsUnlockable => true;
        public override int Difficulty => 1;

        public override List<Unlock> HardcodedRequirements => new()
        {
            (Dish)GDOUtils.GetCustomGameDataObject<MysteryMeatBurgerDish>().GameDataObject
        };

        public override HashSet<Dish.IngredientUnlock> ExtraOrderUnlocks => new()
        {
            new Dish.IngredientUnlock
            {
                Ingredient = GDOUtils.GetCastedGDO<Item, SpecialSauceBottle>(),
                MenuItem = (ItemGroup)GDOUtils.GetExistingGDO(ItemReferences.BurgerPlated)
            },
            new Dish.IngredientUnlock
            {
                Ingredient = GDOUtils.GetCastedGDO<Item, SpecialSauceBottle>(),
                MenuItem = (ItemGroup)GDOUtils.GetExistingGDO(ItemReferences.HotdogPlated)
            },
            new Dish.IngredientUnlock
            {
                Ingredient = GDOUtils.GetCastedGDO<Item, SpecialSauceBottle>(),
                MenuItem = (ItemGroup)GDOUtils.GetExistingGDO(ItemReferences.PiePlated)
            }
        };

        public override HashSet<Item> MinimumIngredients => new()
        {
            GDOUtils.GetCastedGDO<Item, EmptySpecialSauceBottle>()
        };

        public override HashSet<Process> RequiredProcesses => new();

        public override Dictionary<Locale, string> Recipe => new()
        {
            { Locale.English, "Fill bottle with blood and serve when requested. Has 6 uses until a refill is needed" }
        };

        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateUnlockInfo("Special Sauce", "Customers can request the 'special sauce' while eating", null))
        };

        /// <summary>
        /// Emits verbose diagnostics listing every plated item that now allows special sauce orders.
        /// </summary>
        /// <param name="gameDataObject">The dish definition being registered.</param>
        public override void OnRegister(Dish gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int burgerId = GDOUtils.GetExistingGDO(ItemReferences.BurgerPlated).ID;
            int hotdogId = GDOUtils.GetExistingGDO(ItemReferences.HotdogPlated).ID;
            int pieId = GDOUtils.GetExistingGDO(ItemReferences.PiePlated).ID;

            DebugLogSystem.LogVerbose(
                $"SpecialSauceDish registered enabling sauce orders for burgers ({burgerId}), hotdogs ({hotdogId}), and pies ({pieId}).");
        }
    }
}
