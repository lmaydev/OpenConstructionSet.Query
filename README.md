# OpenConstructionSet.Query
CLI tool to load, query and output game data

## Use

-r Resolve dependencies of the provided mods
-g Use the game folders to look for mods and dependencies
-b Load the base game files

-a (mod filename or path) Load this mod as active
-o (path) Output file
-m (multiple mod filenames and/or paths) Load these mods
-f (multiple folder paths) Include these folders while searching for mods

-e (linq expression against a collection of OpenConstructionSet.Models.ItemModels called items.


Example: -rgb -o data.json -e "items.OfType(itemType.BUILDING).Where(i => i.Mod == \"gamedata.base\")"

