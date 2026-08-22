using AgentsSystem;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Agro.Plant.Flower
{
    public enum InfloresenceType { Solitary, Raceme, Spike, Panicle, Umbel, Corymb, Capitulum, Cyme }
    public class FlowerHelper
    {
        private static FlowerSettings _settings;
        public FlowerHelper(FlowerSettings flowerSettings)
        {
            _settings = flowerSettings;
        }
        public void Birth()
        {
            //birth flowerstemm
        }
        public void grow(ref AboveGroundAgent agent, int agentID, PlantSubFormation<AboveGroundAgent> formation)
        {

            switch (agent.Organ)
            {
                case OrganTypes.FlowerStem:
                    {
                        var energyReserve = Math.Clamp(agent.Energy / agent.EnergyStorageCapacity(), 0f, 1f);
                        var growth = 2e-5f * energyReserve * Math.Min(formation.Plant.WaterBalance, energyReserve) * formation.Plant.World.HoursPerTick;
                        var currentSize = new Vector2(agent.Length, agent.Radius);
                        //assure not to outgrow the parent
                        var parentRadius = (agent.Parent >= 0 && !formation.GetIsRizome(agent.Parent)) ? formation.GetBaseRadius(agent.Parent) : float.MaxValue;
                        if (currentSize.Y + growth > parentRadius)
                            growth = parentRadius - currentSize.Y;

                        if (agent.Radius < _settings.fStemRadius)
                            agent.Radius += growth;



                    }
                    break;
                case OrganTypes.FlowerPetiol: { } break;
                case OrganTypes.FlowerPadel: { } break;
                case OrganTypes.FlowerBud: { } break;
                case OrganTypes.FlowerMeristem: {
                        var energyReserve = Math.Clamp(agent.Energy / agent.EnergyStorageCapacity(), 0f, 1f);
                        var growth = new Vector2(1e-3f, 2e-5f) * (  energyReserve * Math.Min(formation.Plant.WaterBalance, energyReserve) * formation.Plant.World.HoursPerTick);
                        var currentSize = new Vector2(agent.Length, agent.Radius);
                        //assure not to outgrow the parent
                        var parentRadius = (agent.Parent >= 0 && !formation.GetIsRizome(agent.Parent)) ? formation.GetBaseRadius(agent.Parent) : float.MaxValue;
                        if (currentSize.Y + growth.Y > parentRadius)
                            growth.Y = parentRadius - currentSize.Y;
                        agent.Length += agent.Length <= 0.01f ? growth.X : 0f;

                        if (agent.Radius < _settings.fStemRadius)
                            agent.Radius += growth.Y;
                    } break;

            }
        }

        // shrinking when max age 
        public void receit(ref AboveGroundAgent agent, int agentID)
        {
            switch (agent.Organ)
            {
                case OrganTypes.FlowerStem:
                    {




                    }
                    break;
                case OrganTypes.FlowerPetiol: { } break;
                case OrganTypes.FlowerPadel: { } break;
                case OrganTypes.FlowerBud: { } break;
                case OrganTypes.FlowerMeristem: { } break;

            }
        }

        public void handleAgent(ref AboveGroundAgent agent, int agentID, PlantSubFormation<AboveGroundAgent> formation, uint timestep)
        {

            if (true)
            {
                if (formation.GetPhase(formation.Plant.Parameters, timestep).Equals(SeasonalPhase.ResetPending))
                {
                    formation.Death(agentID);
                    return;
                }
                grow(ref agent, agentID, formation);
                chaning(ref agent, agentID, formation);
            }
            else
                receit(ref agent, agentID);


        }
    

        private void chaning(ref AboveGroundAgent agent, int agentID, PlantSubFormation<AboveGroundAgent> formation)
        {
            if ((agent.Organ.Equals(OrganTypes.FlowerMeristem) || agent.Organ.Equals(OrganTypes.FlowerBaseBud)) && agent.Length > _settings.stemLengthVar)
            {
                agent.Organ = OrganTypes.FlowerStem;
                //Console.WriteLine($"ttt{agent.FlowerAgent.debth}< {_settings.flowerDebth}");
                if (agent.FlowerAgent.flowerBase)
                {
                    if (agent.FlowerAgent.debth < _settings.flowerBaseDebth)
                    {
                        //Console.WriteLine("base");
                        var or = AboveGroundAgent.TurnUpwards(agent.Orientation);
                        var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = agent.FlowerAgent.debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime, flowerBase = true };
                        var mari = new AboveGroundAgent(formation.Plant, agentID, OrganTypes.FlowerMeristem, or, 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = _settings.LateralAngle, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent };
                        var meristem = formation.Birth(mari);
                        agent.Energy *= 0.9f;
                        agent.Water_g *= 0.9f;
                        if (true) {
                            agent.CreateFlowerBaseLeaves(agent, formation.Plant, 25f, meristem, MorphologyParams.LeafLayout(formation.Plant.Parameters));
                        }

                        return;
                    }
                    else if (_settings.flowerDebth <= 0)
                    {
                        createFlower(ref agent, agentID, formation, 3, agent.Orientation, 0.3f);
                    } else if (false)
                    {
                        var lateralPitch = _settings.LateralAngle + _settings.LateralRoll;
                        var or = AboveGroundAgent.TurnUpwards(agent.Orientation);
                        // Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _settings.LateralAngle * MathF.PI) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (MathF.Tau / _settings.LateralsPerNode) * lat);
                        var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth =  1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime,flowerBase=false };
                        var flowerStem = new AboveGroundAgent(formation.Plant, agentID, OrganTypes.FlowerMeristem, agent.RandomOrientation(formation.Plant, formation.Plant.Parameters, or), 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent, Length = 0.025f, Radius = 0.00025f };
                        var meristem = formation.Birth(flowerStem);
                        agent.Energy *= 0.9f;
                        agent.Water_g *= 0.9f;
                        return;
                    }

                    //laterals mit tiefen wahrscheinlichkeit

                }
                var debth = agent.FlowerAgent.flowerBase ? 0 : agent.FlowerAgent.debth;
                if (debth < _settings.flowerDebth)
                {
                    //Console.WriteLine("flower");
                    if (true)
                    {
                        if (_settings.internodeFlowerWithStem)
                        {
                            var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime };

                            var lateralPitch = _settings.LateralAngle + _settings.LateralRoll;
                            var or = AboveGroundAgent.TurnUpwards(agent.Orientation);
                            // Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _settings.LateralAngle * MathF.PI) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (MathF.Tau / _settings.LateralsPerNode) * lat);

                            var flowerStem = new AboveGroundAgent(formation.Plant, agentID, OrganTypes.FlowerStem, agent.RandomOrientation(formation.Plant, formation.Plant.Parameters, or), 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent };
                            var meristem = formation.Birth(flowerStem);
                            agent.Energy *= 0.9f;
                            agent.Water_g *= 0.9f;
                            createFlower(ref flowerStem, meristem, formation, 3, agent.Orientation, 0.3f); // straight
                        } else
                            createFlower(ref agent, agentID, formation, 3, agent.Orientation, 0.3f); // straight


                    }
                    else
                    {
                        //Console.WriteLine($"up {agentID}");
                        var lateralPitch = _settings.LateralAngle + _settings.LateralRoll;

                        var or = AboveGroundAgent.TurnUpwards(agent.Orientation);
                        var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime };
                        var meristem = formation.Birth(new(formation.Plant, agentID, OrganTypes.FlowerMeristem, or, 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent, Length = 0.1f, Radius = 0.0025f });
                        agent.Energy *= 0.9f;
                        agent.Water_g *= 0.9f;
                    }
                    if (_settings.LateralsPerNode > 0)
                    {
                        //Console.WriteLine($"lat{agentID}");
                        for (var lat = 0; lat < _settings.LateralsPerNode; lat++)
                        {
                            float t = (_settings.flowerDebth <= 0f) ? 1f : Math.Clamp(debth / _settings.flowerDebth, 0, 1);
                            float depthMultiplier = (1f - _settings.floralDepthFactor) + (_settings.floralDepthFactor * t); ;

                            float p = _settings.pFlowerDebth * depthMultiplier;
                            if (formation.Plant.RNG.NextFloat(0, 1) < p)
                            {
                                //createFlower(); //lat roll lat deg
                            }
                            
                            var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime };

                           
                            var or = AboveGroundAgent.TurnUpwards(agent.Orientation);
                            var lateralPitch = 0.3f * MathF.PI;
                            // Quaternion.CreateFromAxisAngle(Vector3.UnitZ, _settings.LateralAngle * MathF.PI) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (MathF.Tau / _settings.LateralsPerNode) * lat);

                            var orientation1 = or * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (MathF.Tau / _settings.LateralsPerNode) * lat) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.10f * MathF.PI);

                            var meristem = formation.Birth(new(formation.Plant, agentID, OrganTypes.FlowerMeristem, agent.RandomOrientation(formation.Plant, formation.Plant.Parameters, orientation1), 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent });
                            agent.Energy *= 0.9f;
                            agent.Water_g *= 0.9f;
                        }
                    }

                }
                else
                {

                    createFlower(ref agent, agentID, formation, 4, AboveGroundAgent.TurnUpwards(agent.Orientation), 0.35f);

                }
            }
        }

        private void createFlower(ref AboveGroundAgent agent, int agentID, PlantSubFormation<AboveGroundAgent> formation, int clustersize, Quaternion baseOr, float clusterAngle)
        {
            var lateralPitch = _settings.LateralAngle + _settings.LateralRoll;
            var rng = formation.Plant.RNG;
            for (var c = 1; c <= clustersize; c++)
            {
                var or = baseOr * Quaternion.CreateFromAxisAngle(Vector3.UnitX, rng.NextPositiveFloat(MathF.Tau)) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -rng.NextPositiveFloat(clusterAngle) * MathF.PI); 
                var floweragent = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = agent.FlowerAgent.debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime };
                var meristem = formation.Birth(new(formation.Plant, agentID, OrganTypes.FlowerPetiol, or, 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent, Length = 0.025f, Radius = 0.00025f });
                agent.Energy *= 0.9f;
                agent.Water_g *= 0.9f;

               
                var floweragentbud = new Flower() { BirthTime = agent.FlowerAgent.BirthTime, debth = agent.FlowerAgent.debth + 1, FlowerStartTime = agent.FlowerAgent.FlowerStartTime };
                var bud = new AboveGroundAgent(formation.Plant, meristem, OrganTypes.FlowerBud, or, 0.1f * agent.Energy, initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { Water_g = 0.1f * agent.Water_g, LateralAngle = lateralPitch, DominanceLevel = agent.DominanceLevel, FlowerAgent = floweragent, Length = 0.0007f, Radius = 0.00035f };
                var budId = formation.Birth(bud);
                agent.Energy *= 0.9f;
                agent.Water_g *= 0.9f;

                int petalCount = 6;
                float angleStep = MathF.Tau / petalCount;

                float basePetalPitch = 0.95f;      // how much petals tilt outward/down
                float pitchVarRange = 0.05f;      // random pitch jitter
                float rollVarRange = 0.05f;      // random roll jitter
               // Quaternion budBase = AboveGroundAgent.TurnUpwards(agent.Orientation);
                

                for (int i = 0; i < petalCount; i++)
                {
                    // Ring angle around bud
                    float a = i * angleStep;

                    // Like your leaf variance
                    float roll = rng.NextFloatVar(rollVarRange);
                    float pitch = rng.NextFloatVar(pitchVarRange);
                    Quaternion o = or * Quaternion.CreateFromAxisAngle(Vector3.UnitX, a) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (basePetalPitch + pitch)) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, roll);
                    float rim = bud.Radius * 0.85f;


                    Vector3 offset = Vector3.Transform(new Vector3(0f, MathF.Cos(a) * rim, MathF.Sin(a) * rim), or);
                    var flower = formation.Birth(new(formation.Plant, budId, OrganTypes.FlowerPadel, o, agent.Energy * (0.1f / petalCount), initialResources: formation.DailyResourceMax, initialProduction: formation.DailyProductionMax) { DominanceLevel = agent.DominanceLevel, Length = 0.005f, Radius = 0.0025f, BaseOffset = offset });

                    agent.Energy *= 0.99f;
                }
            }
           
        }
    }

    public struct Flower
    {
        public bool flowerBase { get; set; } = false;

        public Flower()
        {
        }

        public int debth { get; set; }
        public uint BirthTime { get;  set; }

        public uint FlowerStartTime { get; set; }
    }
}
