import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class PhaseInputNode extends ClassicPreset.Node {
    constructor() {
        super("Phase Input");
        this.addOutput("preFlower", new ClassicPreset.Output(boolSocket, "Is pre-flower"));
        this.addOutput("flowering", new ClassicPreset.Output(boolSocket, "Is flowering"));
        this.addOutput("postFlower", new ClassicPreset.Output(boolSocket, "Is post-flower"));
        this.addOutput("resetPending", new ClassicPreset.Output(boolSocket, "Is reset pending"));
    }
}