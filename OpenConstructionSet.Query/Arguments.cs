using CommandLine;
using OpenConstructionSet.Models;

namespace OpenConstructionSet.Query
{
    partial class Program
    {
        private class Arguments
        {
            [Value(0, HelpText = "A Linq expressions to execute against an IEnummerable<Item> called items", Required = false)]
            public string? Expression { get; set; }

            [Option(shortName: 'i', longName: "installation", Required = false, HelpText = "Which installation to use. Steam, Gog or Local. Omitting this value will cause the first discoverable installation to be used.")]
            public string? Installation { get; set; }

            [Option(shortName: 'm', longName: "mods", Required = false, HelpText = "One or more mods to load")]
            public IEnumerable<string>? Mods { get; set; }

            [Option(shortName: 'g', longName: "load-game-files", Required = false, HelpText = "Load the game's data files.")]
            public bool LoadGameFiles { get; set; }

            [Option(shortName: 'e', longName: "load-enabled-mods", Required = false, HelpText = "Load the game's enabled mods.")]
            public bool LoadEnabledMods { get; set; }

            [Option(shortName: 'o', longName: "output", Required = false, HelpText = "The output file. Defaults to data.json")]
            public string? OutputFile { get; set; }
        }
    }
}
