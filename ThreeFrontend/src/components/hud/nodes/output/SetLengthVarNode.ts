import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class SetLengthVarNode extends ClassicPreset.Node {
    constructor() {
        super("Set Length Var");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
