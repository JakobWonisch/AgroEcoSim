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
        this.addOutput("isRizome", new ClassicPreset.Output(boolSocket, "Is rhizome"));
        this.addOutput("trySpawn", new ClassicPreset.Output(boolSocket, "Is try spawn"));
    }

    data() {
        return {
            energy: 0,
            water: 0,
            length: 0,
            radius: 0,
            wood: 0,
            ageHours: 0,
            isRizome: false,
            trySpawn: false,
        };
    }
}
