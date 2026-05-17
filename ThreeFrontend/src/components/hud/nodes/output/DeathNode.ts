import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class DeathNode extends ClassicPreset.Node {
    constructor() {
        super("Death");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }

    data() {
        return {};
    }
}
