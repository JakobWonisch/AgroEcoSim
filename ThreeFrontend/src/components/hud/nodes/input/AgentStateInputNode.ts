import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class AgentStateInputNode extends ClassicPreset.Node {
    constructor() {
        super("Agent State Input");
        this.addOutput("energy", new ClassicPreset.Output(numSocket, "Energy"));
        this.addOutput("water", new ClassicPreset.Output(numSocket, "Water"));
        this.addOutput("length", new ClassicPreset.Output(numSocket, "Length"));
        this.addOutput("radius", new ClassicPreset.Output(numSocket, "Radius"));
        this.addOutput("wood", new ClassicPreset.Output(numSocket, "Wood"));
        this.addOutput("ageHours", new ClassicPreset.Output(numSocket, "Age (hours)"));
        this.addOutput("lengthVar", new ClassicPreset.Output(numSocket, "Length var"));
        this.addOutput("radiusVar", new ClassicPreset.Output(numSocket, "Radius var"));
        this.addOutput("growthTimeVar", new ClassicPreset.Output(numSocket, "Growth time var"));
        this.addOutput("dominanceLevel", new ClassicPreset.Output(numSocket, "Dominance level"));
        this.addOutput("parentRadiusAtBirth", new ClassicPreset.Output(numSocket, "Parent radius at birth"));
        this.addOutput("previousDayEnvResources", new ClassicPreset.Output(numSocket, "Prev day env resources"));
        this.addOutput("previousDayProductionInv", new ClassicPreset.Output(numSocket, "Prev day production inv"));
        this.addOutput("energyStorageCapacity", new ClassicPreset.Output(numSocket, "Energy storage capacity"));
        this.addOutput("wasMeristemThisTick", new ClassicPreset.Output(boolSocket, "Was meristem this tick"));
        this.addOutput("isRizome", new ClassicPreset.Output(boolSocket, "Is rhizome"));
        this.addOutput("trySpawn", new ClassicPreset.Output(boolSocket, "Is try spawn"));
        this.addOutput("rizomeDepth", new ClassicPreset.Output(numSocket, "Rizome depth"));
        this.addOutput("rizomeTest", new ClassicPreset.Output(boolSocket, "Rizome test"));
        this.addOutput("rizomeTest2", new ClassicPreset.Output(boolSocket, "Rizome test2"));
        this.addOutput("rizomeTest3", new ClassicPreset.Output(boolSocket, "Rizome test3"));
        this.addOutput("rizomeTest4", new ClassicPreset.Output(boolSocket, "Rizome test4"));
    }
}