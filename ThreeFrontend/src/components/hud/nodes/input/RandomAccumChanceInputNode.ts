import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class RandomAccumChanceInputNode extends ClassicPreset.Node {
    constructor() {
        super("Random Accum Chance Input");
        this.addInput("p", new ClassicPreset.Input(numSocket, "Probability"));
        this.addOutput("out", new ClassicPreset.Output(boolSocket, "Is success"));
    }
}
