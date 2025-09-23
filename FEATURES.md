<a id="top"></a>
[← Back to README](README.md)

# Feature Catalog

## Gameplay Systems [🔝](#top)
- Murder loop that lets staff execute diners with the Meat Cleaver, spawn corpses, and splatter gore.
- Suspicion meters that react to illegal sights, escalate to alerts, and end a day if an alerted diner escapes.
- Illegal sight decay that rots corpses and blood overnight while leaving remaining corpse portions intact.
- Blood-to-sauce cycle that lets cleaners bottle spills into special sauce charges and remembers the last guest served.
- Poisoning channel that applies poison from bottles to held food manually or through automated interactors with dedicated audio.
- Process gore rules that let specific recipes spawn blood messes during work, including corpse carving.
- Mince refinement that enables kneading raw mince into uncooked burger patties for the Mystery Meat menu.

## Items [🔝](#top)
- Meat Cleaver: equippable tool that murders customers and boosts chopping speed.
- Mystery Meat: harvestable ingredient from corpses that supports chopping and grinding.
- Rotten Mystery Meat: spoiled variant produced when stored bodies decay.
- Customer Corpse: five-portion carcass that drips blood while being processed and counts as an illegal sight.
- Rotten Customer Corpse: decayed carcass that portions into rotten meat and remains suspicious.
- Casings: stackable ingredient used when forming raw hotdogs.
- Special Sauce Bottle: limited-use condiment that holds six charges and refills from bottled blood.
- Empty Special Sauce Bottle: refillable shell supplied before the sauce is brewed.
- Poison Bottle: reusable vessel that flags contaminated food as poisoned.

## Appliances and Environmental Objects [🔝](#top)
- Manual Meat Grinder that accepts grindable items, plays preference-controlled audio, and outputs through a repositioned hold point.
- Automatic Meat Grinder that runs the grind process unattended, conveys outputs, and emits grinder audio.
- Meat Cleaver Provider that spawns a single cleaver on a thin counter.
- Casings Provider that supplies unlimited casings after purchasing the cleaver station.
- Poison Provider that dispenses poison bottles with holder storage.
- Special Sauce Provider that issues empty bottles and supports prep processes.
- Customer Floor Corpse appliance that supplies corpse items, flags illegal sighting, and transitions into a rotten corpse overnight.
- Rotten Customer Floor Corpse appliance that yields rotten corpses while staying suspicious.
- Blood Spill messes (three stages) that slow players, require cleaning time, stack in place, and can be bottled for sauce.

## Item Groups and Recipes [🔝](#top)
- Raw Hotdog assembly combining mince with a casing before cooking.
- Uncooked Pie assembly pairing raw pie crust with mystery meat prior to baking.

## Dishes and Unlocks [🔝](#top)
- Mystery Meat Burgers as the base restaurant dish, blocking standard meat providers and defining the fresh-meat workflow.
- Mystery Meat Hotdogs as a main course unlocked after burgers, requiring casings and the grind process.
- Mystery Meat Pies as a main course unlocked after burgers, using mystery meat within existing pie steps.
- Special Sauce as an extra course that adds refillable condiment requests for plated mains.
- Mystery Meat recipe entry that delivers the clandestine preparation instructions without being draftable.

## Status Cards and Effects [🔝](#top)
- Cautious Crowd status that shortens suspicion timers by half.
- Messy Murder status that increases the number of blood spills spawned by a kill.
- Persistent Corpses status that keeps corpses between days and lets them rot instead of despawning.

## Visuals and Audio [🔝](#top)
- Suspicion indicator view that swaps between suspicion and alert icons, plays volume-controlled loops, and always faces the camera.
- Meat grinder view that moves and scales the held item to illustrate grind progress.
- Limited-use bottle view that swaps bottle and liquid materials to show remaining charges.
- Dedicated stab, poison, and alert sound events registered with preference-driven volume control.

## Preferences and Tooling [🔝](#top)
- In-game preference sliders covering meat grinder, stab, suspicion, and alert audio levels.
- Preference toggles that gate the Cautious Crowd, Messy Murder, and Persistent Corpses status cards.
- Debug log level preference that gates informational and verbose logging routed through the custom debug helper.
- Fallback sprite asset injection so grind icons display correctly within text meshes.

[Return to README](README.md)
