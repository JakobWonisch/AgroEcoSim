import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class AccumulateProductionNode extends ClassicPreset.Node {
    constructor() {
        super("Accumulate Production");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }

    data() {
        return {};
    }
}
