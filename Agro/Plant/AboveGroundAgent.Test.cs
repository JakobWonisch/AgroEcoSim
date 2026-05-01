using System.Numerics;

namespace Agro;

public partial struct AboveGroundAgent
{
    public partial struct Test
    {

        public static void Tick(ref AboveGroundAgent agent, PlantSubFormation<AboveGroundAgent> formation, int agentID, uint timestep)
        {
            // Console.WriteLine("Test.Tick");

            var plant = formation.Plant;
            var species = plant.Parameters;
            var world = plant.World;

            var lifeSupportPerTick = agent.LifeSupportPerTick(world);


            if (agent.Organ == OrganTypes.Stem)
            {


                agent.Length += 0.01f;
                agent.Radius += 0.001f;

                // Console.WriteLine("lengt: " + agent.Length + " " + agent.Radius);
            }
        }
    }
}
