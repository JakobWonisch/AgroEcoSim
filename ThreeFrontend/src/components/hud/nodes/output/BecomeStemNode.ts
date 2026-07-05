import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class BecomeStemNode extends ClassicPreset.Node {
    constructor() {
        super("Become Stem");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}