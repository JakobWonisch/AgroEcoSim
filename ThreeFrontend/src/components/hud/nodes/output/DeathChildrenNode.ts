import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class DeathChildrenNode extends ClassicPreset.Node {
    constructor() {
        super("Death Children");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}