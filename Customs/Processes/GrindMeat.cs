using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Appliances;
using KitchenMysteryMeat.Systems.Logging;

namespace KitchenMysteryMeat.Customs.Processes
{
    /// <summary>
    /// Registers the bespoke grind process so grinders convert corpses into minced meat with dedicated localisation.
    /// </summary>
    public class GrindMeat : CustomProcess
    {
        public override string UniqueNameID => "GrindMeat";
        public override GameDataObject BasicEnablingAppliance => (Appliance)GDOUtils.GetCustomGameDataObject<ManualMeatGrinder>().GameDataObject;
        public override bool CanObfuscateProgress => true;

        public override List<(Locale, ProcessInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateProcessInfo("Grind", "<sprite name=\"grindmeat\">") )
        };

        /// <summary>
        /// Emits verbose diagnostics highlighting the default enabling appliance for the grind process.
        /// </summary>
        /// <param name="gameDataObject">The process definition being registered.</param>
        public override void OnRegister(Process gameDataObject)
        {
            base.OnRegister(gameDataObject);

            int manualGrinderId = GDOUtils.GetCustomGameDataObject<ManualMeatGrinder>().ID;

            DebugLogSystem.LogVerbose(
                $"GrindMeat process registered with manual grinder enabling appliance {manualGrinderId}.");
        }
    }
}
