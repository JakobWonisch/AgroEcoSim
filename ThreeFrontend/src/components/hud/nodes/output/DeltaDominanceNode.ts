import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class DeltaDominanceNode extends ClassicPreset.Node {
    constructor() {
        super("Delta Dominance");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("count", new ClassicPreset.Input(numSocket, "count"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
