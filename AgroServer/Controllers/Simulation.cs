using AgroServer.Models;
using Agro;
using System.Diagnostics;
using AgroServer.Services;
using System.Diagnostics.CodeAnalysis;

namespace AgroServer.Controllers;

public class SimulationController// : ControllerBase
{
    [RequiresUnreferencedCode("SimulationController")]
    public static void Map(RouteGroupBuilder api, IConfiguration configuration, ISimulationUploadService uploadService, ITerrainBuffer terrainBuffer)
    {
        api.MapGet("/", () => Results.Ok());

        api.MapPost("/", (SimulationRequest request) =>
        {
            var world = Initialize.World(request);
            world.Irradiance.SetAddress(configuration["RendererIPMitsuba"], configuration["RendererPortMitsuba"], configuration["RendererIPTamashii"], configuration["RendererPortTamashii"], request?.RenderMode ?? 0);

            var start = DateTime.UtcNow.Ticks;
            world.Run((uint)world.TimestepsTotal());
            var stop = DateTime.UtcNow.Ticks;
            Debug.WriteLine($"Simulation time: {(stop - start) / TimeSpan.TicksPerMillisecond} ms");

            var response = new SimulationResponse() { Plants = new(world.Count) };
            world.ForEach(formation =>
            {
                if (formation is PlantFormation2 plant)
                    //plantData.Add(@$"{{""P"":{JsonSerializer.Serialize(new Vector3Data(plant.Position))},""V"":{plant.AG.GetVolume()}}}");
                    response.Plants.Add(new() { Volume = plant.AG.GetVolume() });
            });

            Debug.WriteLine($"RENDER TIME: {world.Irradiance.ElapsedMilliseconds} ms");

            if (request?.RequestGeometry ?? false)
            {
                var exportVersion = (byte)(5 + (request.DownloadRoots ?? false ? 1 : 0));
                response.Scene = world.ExportToStream(exportVersion);
            }

            response.Renderer = world.RendererName;

            return response;
        });

        api.MapPost("/upload", (SimulationRequest request) =>
        {
            //TODO Validate the regex
            return uploadService.Add(request);
        });

        api.MapPost("/terrain", (ImportedObjData data) => terrainBuffer.Add(data));

        //Returns a listing of all predefined species
        api.MapGet("/species", () => SpeciesSettings.Predefined);

        //Updates a species
        api.MapPost("/species", (ExportedGraph data) =>
        {
            // TODO: implement species update
        });

        //Returns a listing of all predefined behaviors
        api.MapGet("/behaviors", () => Enum.GetNames<Behavior>());
    }
}