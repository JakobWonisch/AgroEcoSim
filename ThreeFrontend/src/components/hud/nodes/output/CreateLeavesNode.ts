import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class CreateLeavesNode extends ClassicPreset.Node {
    constructor() {
        super("Create Leaves");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }

    data() {
        return {};
    }
}
