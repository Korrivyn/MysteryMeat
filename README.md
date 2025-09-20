# MysteryMeat

## Local build setup

1. Install [PlateUp!](https://store.steampowered.com/app/1599600/PlateUp/) through Steam so the managed game assemblies and workshop content are available locally.
2. Copy `Directory.Build.props.sample` to `Directory.Build.props` in the repository root. The real file stays untracked so each developer can point to their own install.
3. Edit `Directory.Build.props` and set the following properties to match your machine:
   - `<PlateUpManagedDir>` should point at the game's managed assemblies folder, e.g. `C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed` on Windows.
   - `<PlateUpWorkshopDir>` should point at Steam's workshop content directory for PlateUp!, e.g. `C:\Program Files (x86)\Steam\steamapps\workshop\content99600`.
4. Build the solution with `dotnet build MysteryMeat.sln`.

The build will emit a clear error if either property is missing so you know to update your configuration.

## Required workshop packages

The project expects the following workshop items to exist under `$(PlateUpWorkshopDir)`:

- `2898033283` – Harmony (`0Harmony.dll`)
- `3306089551` – KitchenPlatePatch (`KitchenPlatePatch.dll`)
- `2898069883` – KitchenLib (`KitchenLib-Workshop.dll`)
- `2949018507` – PreferenceSystem (`PreferenceSystem-Workshop.dll`)

You can find these IDs in Steam by opening each mod's workshop page and copying the trailing number from the URL. Once Steam has downloaded them, ensure the directory names above exist beneath your configured `PlateUpWorkshopDir`.
