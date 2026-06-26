import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class MakeBudNode extends ClassicPreset.Node {
    constructor() {
        super("Make Bud");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}