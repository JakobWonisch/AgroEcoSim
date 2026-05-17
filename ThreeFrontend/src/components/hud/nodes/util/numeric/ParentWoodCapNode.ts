import { ClassicPreset } from "rete";
import { numSocket } from "../../Sockets";

export class ParentWoodCapNode extends ClassicPreset.Node {
    constructor() {
        super("Parent Wood Cap");
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
        this.addOutput("out", new ClassicPreset.Output(numSocket, "out"));
    }

    data() {
        return { out: 0 };
    }
}
