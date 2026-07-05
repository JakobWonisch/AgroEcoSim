import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class DeathParentNode extends ClassicPreset.Node {
    constructor() {
        super("Death Parent");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}