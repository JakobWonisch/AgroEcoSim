import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class TurnUpwardsNode extends ClassicPreset.Node {
    constructor() {
        super("Turn Upwards");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
