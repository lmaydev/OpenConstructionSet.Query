using forgotten_construction_set;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using OpenConstructionSet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OpenConstructionSet.Query
{
    using C = Func<IEnumerable<ItemModel>, IEnumerable<object>>;

    class Program
    {
        static void Main(string[] args)
        {
            var input = QueryData.Parse(args);

            var gameData = OcsHelper.Load(input.Mods, input.ActiveMod, input.Folders, input.ResolveDependencies, input.LoadGameFiles);

            var data = input.Selector(gameData.items.Values.ToModels());

            System.IO.File.WriteAllText(input.OutputFile, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        delegate IEnumerable<object> Selector(IEnumerable<ItemModel> items);

        private class QueryData
        {
            public string ActiveMod { get; set; }

            public List<GameFolder> Folders { get; set; } = new List<GameFolder>();

            public List<string> Mods { get; set; } = new List<string>();

            public bool ResolveDependencies { get; set; }

            public bool LoadGameFiles { get; set; }

            public Selector Selector { get; set; }

            public string OutputFile { get; set; } = "data.json";

            public QueryData()
            {
            }

            public QueryData(string[] args)
            {

            }

            public static QueryData Parse(string[] args)
            {
                var result = new QueryData();

                string expression = null;

                bool useGameFolders = false;

                char? currentSwitch = null;

                for (var i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    if (arg.StartsWith("-"))
                    {
                        currentSwitch = null;

                        foreach (var letter in arg.Substring(1))
                        {
                            switch (letter)
                            {
                                case 'r':
                                    result.ResolveDependencies = true;
                                    break;
                                case 'g':
                                    useGameFolders = true;
                                    break;
                                case 'b':
                                    result.LoadGameFiles = true;
                                    break;
                                default:
                                    currentSwitch = letter;
                                    break;
                            }
                        }
                    }
                    else if (currentSwitch != null)
                    {
                        switch (currentSwitch)
                        {
                            case 'a':
                                result.ActiveMod = arg;
                                break;
                            case 'e':
                                expression = arg;
                                break;
                            case 'o':
                                result.OutputFile = arg;
                                break;
                            case 'm':
                                result.Mods.Add(arg);
                                break;
                            case 'f':
                                result.Folders.Add(GameFolder.Data(arg));
                                result.Folders.Add(GameFolder.Mod(arg));
                                break;
                        }
                    }
                    else
                    {
                        expression = arg;
                    }
                }

                if (useGameFolders && OcsSteamHelper.TryFindGameFolders(out var folders))
                {
                    result.Folders.AddRange(folders.ToArray());
                }

                result.Selector = (expression == null) ? new Selector(items => items) :
                                                         new Selector(BuildSelector());

                return result;

                C BuildSelector()
                {
                    const string prefix = "items =>";

                    if (!expression.StartsWith(prefix))
                    {
                        expression = prefix + expression;
                    }

                    var options = ScriptOptions.Default.AddReferences(typeof(GameData).Assembly)
                                                       .AddReferences(typeof(Enumerable).Assembly)
                                                       .AddReferences(typeof(OcsHelper).Assembly)
                                                       .AddImports("System.Linq", "forgotten_construction_set", "OpenConstructionSet");

                    return CSharpScript.EvaluateAsync<C>(expression, options).Result;
                }
            }
        }
    }
}
