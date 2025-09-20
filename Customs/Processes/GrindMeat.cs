using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using KitchenMysteryMeat.Customs.Appliances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitchenMysteryMeat.Customs.Processes
{
    public class GrindMeat : CustomProcess
    {
        // The unique internal name of this process
        public override string UniqueNameID => "GrindMeat";

        // The "default" appliance of this process (e.g., counter for "chop", hob for "cook")
        // When a dish requires this process, this is the appliance that will spawn at the beginning of a run
        public override GameDataObject BasicEnablingAppliance => (Appliance)GDOUtils.GetCustomGameDataObject<ManualMeatGrinder>().GameDataObject;

        // Whether or not the process can be obfuscated, such as through the "Blindfolded Chefs" card. This is normally set to `true`
        public override bool CanObfuscateProgress => true;

        // The localization information for this process. This must be set for at least one language. 
        [Obsolete("Use the Fully Loaded localisation APIs.", false)]
        public override LocalisationObject<ProcessInfo> Info
        {
            get
            {
                var info = new LocalisationObject<ProcessInfo>();

                info.Add(Locale.English, LocalisationUtils.CreateProcessInfo("Grind", "<sprite name=\"grindmeat\">"));

                return info;
            }
        }
    }
}
