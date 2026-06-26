import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class SetEnergyNode extends ClassicPreset.Node {
    constructor() {
        super("Set Energy");
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
    }
}