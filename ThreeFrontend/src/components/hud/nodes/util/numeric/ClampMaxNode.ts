import { ClassicPreset } from "rete";
import { numSocket } from "../../Sockets";

export class ClampMaxNode extends ClassicPreset.Node {
    constructor() {
        super("Clamp Max");
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
        this.addInput("max", new ClassicPreset.Input(numSocket, "max"));
        this.addOutput("out", new ClassicPreset.Output(numSocket, "out"));
    }
}