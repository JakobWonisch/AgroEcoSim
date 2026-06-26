import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class MultiplyWaterNode extends ClassicPreset.Node {
    constructor() {
        super("Multiply Water");
        this.addInput("factor", new ClassicPreset.Input(numSocket, "factor"));
    }
}