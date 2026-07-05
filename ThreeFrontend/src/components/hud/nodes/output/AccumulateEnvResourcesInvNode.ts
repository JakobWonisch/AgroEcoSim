import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class AccumulateEnvResourcesInvNode extends ClassicPreset.Node {
    constructor() {
        super("Accumulate Env Resources Inv");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }
}
