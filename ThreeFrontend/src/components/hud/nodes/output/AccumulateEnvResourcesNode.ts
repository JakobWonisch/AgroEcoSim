import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class AccumulateEnvResourcesNode extends ClassicPreset.Node {
    constructor() {
        super("Accumulate Env Resources");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }
}
