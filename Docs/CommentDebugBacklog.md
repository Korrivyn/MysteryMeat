# Comment and Debug Coverage Backlog (Sept 24, 2025 @ 3:30 a.m. CST)

This checklist captures every Mystery Meat code file that still lacks the required summary comments and/or integration with the updated debug logging system. Use it as the authoritative backlog for finishing the current documentation and diagnostics push.

## Components
- `Components/CEmptyBottle.cs` — Struct has no summary describing the empty bottle marker or how `FullBottleID` routes refills.
- `Components/CFillsBottle.cs` — Missing summary on the appliance-side filler component and its `BottleID` contract.
- `Components/CGrindable.cs` — Lacks a summary clarifying that it tags meat that should feed grinder processes.
- `Components/CIllegalSight.cs` — Needs a summary covering illegal sight tracking and `TurnIntoOnDayStart` behavior.
- `Components/CIllegalSightHolderPreserved.cs` — No summary documenting why the holder flag is preserved overnight.
- `Components/CKilled.cs` — Missing explanation of the `Bloody` marker applied to dispatched customers.
- `Components/CKillsCustomer.cs` — Needs summary describing how it marks tools that can kill customers.
- `Components/CLimitedUseBottle.cs` — Requires documentation of the remaining charges state for limited bottles.
- `Components/CMeatGrinder.cs` — Needs a summary that outlines grinder process IDs and input/output offsets.
- `Components/CPersistPortions.cs` — Lacks a summary for the overnight portion preservation toggle.
- `Components/CPoisonBottle.cs` — Missing summary on the poison bottle tag for exchange logic.
- `Components/CPoisoned.cs` — Needs documentation summarizing the poison status marker.
- `Components/CProcessCausesSpill.cs` — Summary required for spill metadata (process, mess ID, rate, overwrite flag).
- `Components/CSuspicionIndicator.cs` — Missing summary on suspicion UI state tracking.
- `Components/CTrashBag.cs` — Needs summary covering trash bag capacity and state fields.
- `Components/CTurnIntoItem.cs` — Requires summary describing the conversion target logic.

## Customs / Appliances
- `Customs/Appliances/BloodSpill1.cs` — Class lacks high-level summary for the mess prefab and no verbose logging around bottle refill enabling.
- `Customs/Appliances/BloodSpill2.cs` — Needs class-level summary and optional debug traces for stack progression behavior.
- `Customs/Appliances/BloodSpill3.cs` — Missing summary for the final blood spill tier and associated cleanup timings.
- `Customs/Appliances/CasingsProvider.cs` — No summary explaining the unlimited casings source or shop requirements.
- `Customs/Appliances/CustomerFloorCorpse.cs` — Needs summary documenting corpse provider behavior and illegal sight handling.
- `Customs/Appliances/RottenCustomerFloorCorpse.cs` — Missing summary for rotten corpse provider variant.
- `Customs/Appliances/TrashBagProvider.cs` — Requires summary of trash bag vending logic and prerequisites.

## Customs / Items
- `Customs/Items/Casing.cs` — Lacks summary for casing item purpose and provider linkage.
- `Customs/Items/EmptySpecialSauceBottle.cs` — Needs summary covering refill workflow for empty bottles.
- `Customs/Items/MeatCleaver.cs` — Missing summary describing cleaver capabilities and kill tagging.
- `Customs/Items/MysteryMeat.cs` — Requires summary highlighting grinder-ready meat flow and processes.
- `Customs/Items/PoisonBottle.cs` — Needs summary for poison bottle behavior and dedicated provider.
- `Customs/Items/RottenMysteryMeat.cs` — Lacks summary documenting rotten variant and visual effects.

## Customs / ItemGroups
- `Customs/ItemGroups/BaggedCorpse.cs` — Entire definition is disabled but still needs summary comments before reactivation.
- `Customs/ItemGroups/RawHotdog.cs` — Missing summary for recipe composition and conversion rules.
- `Customs/ItemGroups/RawPottedLobster.cs` — Commented-out implementation lacks required summaries and debug if restored.

## Customs / Dishes
- `Customs/Dishes/MysteryMeatBurgerDish.cs` — Needs summary articulating unlock purpose and gameplay impact.
- `Customs/Dishes/MysteryMeatHotdogDish.cs` — Missing summary describing dish unlock flow and requirements.
- `Customs/Dishes/MysteryMeatPieDish.cs` — Requires summary covering pie unlock configuration.
- `Customs/Dishes/SpecialSauceDish.cs` — Lacks summary for special sauce unlock metadata.

## Customs / Processes
- `Customs/Processes/GrindMeat.cs` — Needs summary to describe grinder process registration and localization payloads.

## UnityProject Template Editors
- `UnityProject - MysteryMeat/Assets/Template/Editor/AssetBundler.cs` — Add summaries for `BuildAssetBundle`, `GetGameObjectPath`, and maintenance menu actions; integrate preference-aware logging if editor tooling should honor mod settings.
- `UnityProject - MysteryMeat/Assets/Template/Editor/EmptyAtZero_Creator.cs` — Missing summaries for creation helpers invoked from the PlateUp! menu.
- `UnityProject - MysteryMeat/Assets/Template/Editor/EmptyChildAtGlobalZero_Creator.cs` — Needs summary comments for both menu entry points.
- `UnityProject - MysteryMeat/Assets/Template/Editor/EmptyChildAtLocalZero_Creator.cs` — Lacks summaries for the local-zero creation commands.
- `UnityProject - MysteryMeat/Assets/Template/Editor/EmptyCreator.cs` — Requires summaries across helper overloads and should add verbose logging when generating editor prefabs.

Update this list as files are documented and instrumented so the backlog remains accurate.
