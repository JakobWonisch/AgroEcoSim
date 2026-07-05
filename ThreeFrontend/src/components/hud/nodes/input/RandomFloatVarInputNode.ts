import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class RandomFloatVarInputNode extends ClassicPreset.Node {
    constructor() {
        super("Random Float Var Input");
        this.addInput("variance", new ClassicPreset.Input(numSocket, "Variance"));
        this.addOutput("out", new ClassicPreset.Output(numSocket, "Value"));
    }
}
