import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class SetWoodNode extends ClassicPreset.Node {
    constructor() {
        super("Set Wood");
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
    }

    data() {
        return {};
    }
}
