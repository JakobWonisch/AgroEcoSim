import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class RandomChanceInputNode extends ClassicPreset.Node {
    constructor() {
        super("Random Chance Input");
        this.addInput("p", new ClassicPreset.Input(numSocket, "Probability"));
        this.addOutput("out", new ClassicPreset.Output(boolSocket, "Is success"));
    }
}