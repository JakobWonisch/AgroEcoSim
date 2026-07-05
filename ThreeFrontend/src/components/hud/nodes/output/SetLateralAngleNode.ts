import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class SetLateralAngleNode extends ClassicPreset.Node {
    constructor() {
        super("Set Lateral Angle");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
