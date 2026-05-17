import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class DeltaWaterNode extends ClassicPreset.Node {
    constructor() {
        super("Delta Water");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }

    data() {
        return {};
    }
}
