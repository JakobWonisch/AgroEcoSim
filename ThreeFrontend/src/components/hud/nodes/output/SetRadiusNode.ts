import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class SetRadiusNode extends ClassicPreset.Node {
    constructor() {
        super("Set Radius");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
