using System.Text.Json;
using Agro;
using Agro.Testing;
using CommandLine;
using Utils;
using Utils.Json;

internal class Program
{
    public class Options
    {
        [Option('i', "import", Required = false, HelpText = "Import simulation settings from a json file.")]
        public string? ImportFile { get; set; }

        [Option('e', "export", Required = false, HelpText = "Export aggregated results to a json file.")]
        public string? ExportFile { get; set; }

        [Option("record-legacy", Required = false, HelpText = "Record per-tick plant state trace using legacy C# behavior (JSONL).")]
        public string? RecordLegacy { get; set; }

        [Option("record-node", Required = false, HelpText = "Record per-tick plant state trace using behavior graphs (JSONL).")]
        public string? RecordNode { get; set; }

        [Option("compare", Required = false, Min = 2, Max = 2, HelpText = "Compare two JSONL traces (legacy vs node).")]
        public IEnumerable<string>? Compare { get; set; }

        [Option("max-hours", Required = false, HelpText = "Override TotalHours for parity runs.")]
        public int? MaxHours { get; set; }

        [Option("tolerance", Required = false, Default = 1e-5f, HelpText = "Absolute float tolerance for trace comparison.")]
        public float Tolerance { get; set; }

        [Option("ignore-rng", Required = false, HelpText = "Skip per-plant RNG state when comparing traces.")]
        public bool IgnoreRng { get; set; }
    }

    private static int Main(string[] args) => Parser.Default.ParseArguments<Options>(args).MapResult(Run, _ => 1);

    static int Run(Options options)
    {
        var comparePaths = options.Compare?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        if ((options.RecordLegacy != null || options.RecordNode != null) && options.ImportFile == null)
        {
            Console.Error.WriteLine("Record commands require -i/--import with a SimulationRequest JSON fixture.");
            return 1;
        }

        if (comparePaths is { Length: 2 })
        {
            var mismatch = TraceComparer.CompareFiles(comparePaths[0], comparePaths[1], new TraceCompareOptions
            {
                Tolerance = options.Tolerance,
                IgnoreRng = options.IgnoreRng,
            });
            if (mismatch == null)
            {
                Console.WriteLine("Traces match.");
                return 0;
            }
            Console.Error.WriteLine($"Mismatch at timestep {mismatch.Timestep}, path {mismatch.Path}");
            Console.Error.WriteLine($"  expected: {mismatch.Expected}");
            Console.Error.WriteLine($"  actual:   {mismatch.Actual}");
            return 1;
        }

        if (comparePaths is { Length: > 0 })
        {
            Console.Error.WriteLine("--compare requires exactly two trace file paths.");
            return 1;
        }

        if (options.ImportFile != null)
        {
            if (!File.Exists(options.ImportFile))
                throw new FileNotFoundException("Simulation settings file not found.", options.ImportFile);
            var settings = Import.JsonFile<SimulationRequest>(options.ImportFile);

            if (options.RecordLegacy != null)
            {
                SimulationHarness.RecordTrace(settings, BehaviorRunMode.Legacy, options.RecordLegacy, options.MaxHours);
                Console.WriteLine($"Wrote legacy trace to {options.RecordLegacy}");
                return 0;
            }

            if (options.RecordNode != null)
            {
                SimulationHarness.RecordTrace(settings, BehaviorRunMode.Node, options.RecordNode, options.MaxHours);
                Console.WriteLine($"Wrote node trace to {options.RecordNode}");
                return 0;
            }
        }

        AgroWorld world;
        if (options.ImportFile != null)
        {
            var settings = Import.JsonFile<SimulationRequest>(options.ImportFile);
            world = Initialize.World(settings);
        }
        else
            world = Initialize.World();
        var start = DateTime.UtcNow.Ticks;
        world.Run((uint)world.TimestepsTotal());
        var stop = DateTime.UtcNow.Ticks;
        Console.WriteLine($"Simulation time: {(stop - start) / TimeSpan.TicksPerMillisecond} ms");

        if (options.ExportFile != null)
        {
            var plantData = new List<string>();
            world.ForEach(formation =>
            {
                if (formation is PlantFormation2 plant)
                    plantData.Add(@$"{{""P"":{JsonSerializer.Serialize(new Vector3Data(plant.Position))},""V"":{plant.AG.GetVolume()}}}");
            });

            File.WriteAllText(options.ExportFile, $"[{string.Join(",",plantData)}]");
        }

        Console.WriteLine($"RENDER TIME: {world.Irradiance.ElapsedMilliseconds} ms");
        return 0;
    }
}
