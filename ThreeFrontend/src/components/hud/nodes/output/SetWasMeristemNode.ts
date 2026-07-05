import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class SetWasMeristemNode extends ClassicPreset.Node {
    constructor() {
        super("Set Was Meristem");
        this.addInput("value", new ClassicPreset.Input(boolSocket, "value"));
    }
}
