import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class SimulationSettingsInputNode extends ClassicPreset.Node {
    constructor() {
        super("Simulation Settings Input");
        this.addOutput("hoursPerTick", new ClassicPreset.Output(numSocket, "Hours per tick"));
    }
}
