using CommandLine;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using OpenConstructionSet.Data;
using OpenConstructionSet.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenConstructionSet.Query
{
    using Selector = Func<IEnumerable<Item>, object>;

    partial class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (!TryParse(args, out var arguments, out var errors))
            {
                Console.Error.WriteLine("Invalid arguments");
                Console.Error.WriteLine(string.Join(Environment.NewLine, errors));
                return 1;
            }

            Selector selector;

            try
            {
                selector = await Evaluate(arguments.Expression);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error whilst compiling expression");
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }

            var discovery = OcsDiscoveryService.Default;

            Installation? installation = null;

            if (arguments.Installation is not null)
            {
                var locatorName = discovery.SupportedFolderLocators.FirstOrDefault(n => n.Equals(arguments.Installation, StringComparison.OrdinalIgnoreCase));

                if (locatorName is null)
                {
                    Console.Error.WriteLine($"Installation \"{arguments.Installation}\" could not be found");
                    return 3;
                }

                installation = discovery.DiscoverInstallation(locatorName);

                if (installation is null)
                {
                    Console.Error.WriteLine($"Installation \"{arguments.Installation}\" could not be found");
                    return 3;
                }
            }

            // Using GUID as name is required :|
            var options = new OcsDataContexOptions(Guid.NewGuid().ToString(),
                                                   BaseMods: arguments.Mods,
                                                   Installation: installation,
                                                   LoadGameFiles: arguments.LoadGameFiles ? ModLoadType.Base : ModLoadType.None,
                                                   LoadEnabledMods: arguments.LoadEnabledMods ? ModLoadType.Base : ModLoadType.None);

            OcsDataContext context;

            try
            {
                context = OcsDataContextBuilder.Default.Build(options);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error whilst loading data");
                Console.Error.WriteLine(ex.ToString());
                return 4;
            }

            var data = selector(context.Items.Values);

            Stream stream;

            if (arguments.OutputFile is not null)
            {
                try
                {
                    File.Delete(arguments.OutputFile);

                    stream = File.Create(arguments.OutputFile);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error whilst creating output file \"{arguments.OutputFile}\"");
                    Console.Error.WriteLine(ex.ToString());
                    return 5;
                }
            }
            else
            {
                stream = Console.OpenStandardOutput();
            }

            try
            {
                await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error whilst serializing data");
                Console.Error.Write(ex.ToString());
            }

            return 0;
        }

        static Task<Selector> Evaluate(string? expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                expression = "items";
            }

            const string prefix = "items =>";

            if (!expression.StartsWith(prefix))
            {
                expression = prefix + expression;
            }

            var options = ScriptOptions.Default.AddReferences(typeof(Enumerable).Assembly)
                                               .AddReferences(typeof(OcsDataContext).Assembly)
                                               .AddImports("System.Linq", "OpenConstructionSet", "OpenConstructionSet.Models");

            return CSharpScript.EvaluateAsync<Selector>(expression, options);
        }

        static bool TryParse(string[] args, [MaybeNullWhen(false)] out Arguments arguments, [MaybeNullWhen(true)] out IEnumerable<Error> errors)
        {
            var parseResult = new Parser(config => config.HelpWriter = Console.Out).ParseArguments<Arguments>(args);

            Arguments? localArguments = arguments = null;
            IEnumerable<Error>? localErrors = errors = null;

            var success = parseResult.MapResult(
                a =>
                {
                    localArguments = a;
                    localErrors = null;
                    return true;
                },
                e =>
                {
                    localArguments = null;
                    localErrors = e;
                    return false;
                });

            arguments = localArguments;
            errors = localErrors;

            return success;
        }
    }
}
