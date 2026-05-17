import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class BecomeMeristemNode extends ClassicPreset.Node {
    constructor() {
        super("Become Meristem");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }

    data() {
        return {};
    }
}
