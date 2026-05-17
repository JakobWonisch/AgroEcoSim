import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class SetAuxinsNode extends ClassicPreset.Node {
    constructor() {
        super("Set Auxins");
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
    }

    data() {
        return {};
    }
}
