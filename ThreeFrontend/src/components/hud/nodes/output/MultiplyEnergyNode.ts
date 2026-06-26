import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class MultiplyEnergyNode extends ClassicPreset.Node {
    constructor() {
        super("Multiply Energy");
        this.addInput("factor", new ClassicPreset.Input(numSocket, "factor"));
    }
}