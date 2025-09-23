[← Back to README](README.md)

# Mystery Meat QA Rubric

## 1. Mod Configuration and Preference Checks
- **Test: Mystery Meat preference page exposes all controls.**
  - Steps:
    1. Open the in-run pause menu and navigate to the Mystery Meat preferences section.
    2. Confirm the Audio Settings submenu contains sliders for meat grinder, stab, suspicion, and alert volumes.
    3. Confirm the Card Settings submenu lists toggles for Cautious Crowd, Messy Murder, and Persistent Corpses.
    4. Confirm the Debug Settings submenu exposes the debug log level selector.
  - Expected Results:
    - All sliders, toggles, and the debug selector are present and interactive.
    - Each control displays the correct label and default value (50% for audio, Enabled for cards, Off for logging).
- **Test: Card toggles affect unlock availability after a restart.**
  - Steps:
    1. Disable one status card in the Mystery Meat preferences.
    2. Fully restart the game session or reload to apply the change.
    3. Progress far enough to view card offerings.
  - Expected Results:
    - The disabled card does not appear in the unlock pool.
    - Re-enabling the card and restarting restores it to the pool.
- **Test: Audio sliders impact bespoke sound events.**
  - Steps:
    1. Set the relevant slider to 0%.
    2. Trigger the paired event (grinder run, cleaver stab, suspicion loop, or alert siren).
    3. Raise the slider to 100% and repeat the event.
  - Expected Results:
    - Audio is inaudible at 0% and loud at 100% without restarting the level.
- **Test: Debug log preference gates verbosity.**
  - Steps:
    1. Run a service with logging set to Off and monitor the log output.
    2. Switch the preference to Verbose, restart the run, and repeat the same actions (kills, suspicion triggers, sauce servings).
  - Expected Results:
    - No Mystery Meat debug chatter appears when set to Off.
    - Informational and verbose notes appear after elevating the preference.

## 2. Murder Loop and Gore Generation
- **Test: Meat Cleaver kills diners in every seating state.**
  - Steps:
    1. Acquire the cleaver from its provider.
    2. Execute a diner while they wait outside, one while walking to the table, and one seated at the table.
  - Expected Results:
    - Each target drops immediately, plays the stab sound, and spawns a floor corpse appliance in their position.
    - The associated group loses the slain member’s order and reacts according to the suspicion system.
- **Test: Blood spill counts respect Messy Murder.**
  - Steps:
    1. Kill three diners without the Messy Murder status active and count the blood spill appliances spawned per kill.
    2. Enable Messy Murder, restart, repeat the kills, and recount.
  - Expected Results:
    - Without the status, kills spawn zero to two puddles.
    - With the status, each kill spawns at least one puddle and can reach three.
- **Test: Corpses drip blood while portioned.**
  - Steps:
    1. Spawn a fresh corpse and begin carving portions for meat.
  - Expected Results:
    - Blood spills randomly appear near the work location while the body is being processed.
- **Test: Destroyed diners are removed from groups.**
  - Steps:
    1. Kill every member of a seated group.
    2. Observe the group indicator once all corpses are spawned.
  - Expected Results:
    - Orders tied to dead members disappear.
    - The group indicator despawns after all members are dead or have left.

## 3. Suspicion, Alerts, and Loss Conditions
- **Test: Suspicion indicator attaches to every new diner.**
  - Steps:
    1. Start a day and watch arriving diners, including variants such as cats.
  - Expected Results:
    - Every diner carries the floating suspicion indicator object before any incidents occur.
- **Test: Illegal sights trigger suspicion only within view cones.**
  - Steps:
    1. Place a fresh corpse in front of a diner and note the suspicion fill rate.
    2. Move the corpse behind the diner or to another room.
  - Expected Results:
    - Suspicion climbs while the corpse is in front and in the same room.
    - Suspicion recovers once the corpse is hidden or removed from view.
- **Test: Cautious Crowd halves the countdown.**
  - Steps:
    1. With the status disabled, time how long it takes one diner to hit an alert from a direct corpse sighting.
    2. Enable Cautious Crowd, restart, repeat the test with the same setup.
  - Expected Results:
    - The alert threshold arrives roughly twice as fast with Cautious Crowd active.
- **Test: Alert escalation clears orders and starts a flee.**
  - Steps:
    1. Force a diner to reach full suspicion.
  - Expected Results:
    - The indicator swaps to the alert icon, the alert sound plays, outstanding orders for that diner vanish, and the group begins leaving.
- **Test: Escaping alerted diners cost a life.**
  - Steps:
    1. Allow an alerted diner to path all the way off the map.
  - Expected Results:
    - The run loses a life immediately upon exit and the diner despawns.

## 4. Illegal Sight Persistence and Overnight Behaviour
- **Test: Corpses rot without Persistent Corpses.**
  - Steps:
    1. Kill a diner, leave the corpse on the floor, end the day.
  - Expected Results:
    - The corpse and blood puddles are gone the next morning.
- **Test: Corpses persist and rot with the status active.**
  - Steps:
    1. Enable Persistent Corpses, restart, kill a diner, end the day.
  - Expected Results:
    - The fresh corpse remains but is replaced with the rotten visual and supplies rotten corpse items.
- **Test: Portion counts carry over after rotting.**
  - Steps:
    1. Carve part of a corpse, leaving some portions intact.
    2. End the day with Persistent Corpses active.
  - Expected Results:
    - The rotten corpse retains the correct remaining portion count.
- **Test: Genuine preservers stop decay.**
  - Steps:
    1. Store a corpse inside a preserving appliance (for example, a freezer) before ending the day.
  - Expected Results:
    - The corpse does not rot or disappear overnight.
- **Test: Trash bags hold corpses through day change.**
  - Steps:
    1. Bag a corpse, end the day.
  - Expected Results:
    - The trash bag still contains the carcass next day, and the bag visuals reflect the filled state.

## 5. Special Sauce Lifecycle
- **Test: Empty bottles fill from fresh blood.**
  - Steps:
    1. Hold an empty special sauce bottle.
    2. Clean a blood puddle completely.
  - Expected Results:
    - The bottle becomes full, the puddle is removed, and that puddle cannot refill a second bottle.
- **Test: Filled bottle shows six charges.**
  - Steps:
    1. Inspect the bottle after refilling.
  - Expected Results:
    - The bottle view shows liquid occupying all six segments.
- **Test: Charges deduct per extra request.**
  - Steps:
    1. Serve a plated main with the special sauce extra enabled.
    2. Let customers request sauce and deliver it from a table bottle.
  - Expected Results:
    - Each request removes exactly one charge.
    - The same customer cannot consume two charges for the same order.
- **Test: Empty bottles revert to shells.**
  - Steps:
    1. Drain all six charges during service.
  - Expected Results:
    - The bottle converts to the empty version and can be refilled from blood puddles.

## 6. Poisoning Flow
- **Test: Manual poisoning contaminates held food.**
  - Steps:
    1. Hold a poison bottle and interact with a counter holding ready-to-serve food.
  - Expected Results:
    - The food shows the poisoned state and plays the poison sound once.
- **Test: Automation poisoning works.**
  - Steps:
    1. Configure an automated interactor to hold a poison bottle facing a conveyor or appliance with food.
  - Expected Results:
    - The automation applies poison to passing food items and plays the poison sound.
- **Test: Poisoned food has consequences.**
  - Steps:
    1. Serve a poisoned portion to a diner.
  - Expected Results:
    - The diner reacts as intended (for example, collapsing or triggering the murder cleanup flow) once the poisoned food is consumed.
- **Test: Re-poisoning does not stack.**
  - Steps:
    1. Attempt to poison the same item twice.
  - Expected Results:
    - The second attempt does not replay the sound or alter the item further.

## 7. Meat Processing and Dish Coverage
- **Test: Grindable items feed both grinders.**
  - Steps:
    1. Load Mystery Meat into the manual grinder and complete the process.
    2. Repeat with the automatic grinder.
  - Expected Results:
    - Both grinders output mince, respect the grind icon sprite, and reposition the held item according to the view.
- **Test: Ground meat no longer repeats the grind.**
  - Steps:
    1. Attempt to grind mince that just left the grinder.
  - Expected Results:
    - The grinder refuses the processed mince.
- **Test: Mince kneads into raw patties.**
  - Steps:
    1. Knead mince on a counter.
  - Expected Results:
    - A raw burger patty is produced.
- **Test: Mystery Meat Burger recipe path works.**
  - Steps:
    1. Harvest meat from a corpse, grind or knead as required, assemble the burger.
  - Expected Results:
    - The plated burger serves correctly and satisfies a main-course order.
- **Test: Hotdog and pie recipes honour dependencies.**
  - Steps:
    1. Unlock hotdogs and confirm casings are available only after the cleaver provider exists.
    2. Assemble raw hotdogs using mince plus casings, then cook and serve.
    3. Assemble mystery meat pies following the described steps.
  - Expected Results:
    - Each recipe produces the correct plated item and satisfies orders.
- **Test: Special Sauce extra integrates with mains.**
  - Steps:
    1. Unlock the Special Sauce dish.
    2. Serve burgers, hotdogs, and pies to sauce-requesting customers.
  - Expected Results:
    - Each main can accept the special sauce extra without breaking the order flow.

## 8. Blood Spills and Mess Interactions
- **Test: Blood puddle stages stack.**
  - Steps:
    1. Allow multiple spills to spawn in the same location without cleaning.
  - Expected Results:
    - The puddle escalates through the three visual stages and increases clean time.
- **Test: Blood slows movement.**
  - Steps:
    1. Walk through each spill stage.
  - Expected Results:
    - Movement slows according to the spill stage radius.
- **Test: Cleaning without a bottle removes the fill option.**
  - Steps:
    1. Clean a puddle with empty hands.
  - Expected Results:
    - The puddle disappears and no bottle is filled.

## 9. Trash Bag Handling
- **Test: Bag stores a corpse and reveals stage art.**
  - Steps:
    1. Pick up a trash bag, interact with a corpse.
  - Expected Results:
    - The bag closes, the corpse is stored, and the bag’s visual switches to the corpse art showing the correct portion stage.
- **Test: Stored corpse can be retrieved.**
  - Steps:
    1. Interact with the filled bag to withdraw the body.
  - Expected Results:
    - The corpse returns to the player or surface with the same remaining portion count.

## 10. Appliance and Provider Availability
- **Test: Providers respect unlock chains.**
  - Steps:
    1. Attempt to purchase the casings provider before owning the cleaver provider.
    2. Obtain the cleaver provider, then revisit the blueprint offers.
  - Expected Results:
    - The casings provider (and trash bag provider) only appear once the cleaver provider is owned.
- **Test: Special Sauce provider operations.**
  - Steps:
    1. Interact with the provider to collect an empty bottle.
    2. Attempt to run its built-in processes.
  - Expected Results:
    - The provider dispenses bottles and supports the advertised prep interactions without soft locks.
- **Test: Poison provider restocks bottles.**
  - Steps:
    1. Collect the single poison bottle.
    2. Return later in the day.
  - Expected Results:
    - The provider restocks after its cooldown, allowing further bottles to be drawn.

## 11. Visual and Audio Feedback
- **Test: Suspicion indicator always faces the camera.**
  - Steps:
    1. Circle around a diner while the indicator is visible.
  - Expected Results:
    - The indicator billboard remains oriented toward the viewer and swaps between suspicion and alert icons as states change.
- **Test: Grinder view matches progress.**
  - Steps:
    1. Watch a grinder while processing meat.
  - Expected Results:
    - The held item sinks into the machine and scales down as progress nears completion, then returns to the output position.
- **Test: Bottle view mirrors charge count.**
  - Steps:
    1. Use sauce charges one at a time while watching the bottle.
  - Expected Results:
    - Visible liquid segments disappear in sync with the remaining charges.

## 12. Automation and Edge Cases
- **Test: Automated poisoners respect reachability.**
  - Steps:
    1. Set up an automated poisoner facing a blocked tile.
  - Expected Results:
    - The automation logs a warning (if logging is on) and skips the action when the tile is unreachable.
- **Test: Multiple killings in quick succession do not break suspicion.**
  - Steps:
    1. Kill several diners rapidly in the same area.
  - Expected Results:
    - Suspicion indicators update individually and alerts still trigger correctly.
- **Test: Simultaneous sauce requests handle multi-table scenarios.**
  - Steps:
    1. Serve two tables requesting sauce at the same time using separate bottles.
  - Expected Results:
    - Each table consumes the correct number of charges without cross-contamination or resets.

## 13. User Interface Assets
- **Test: Grind sprite appears in process prompts.**
  - Steps:
    1. View any prompt that references the grind process.
  - Expected Results:
    - The grind icon renders correctly without missing-glyph markers.

[Return to README](README.md)