import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class SetEnergyNode extends ClassicPreset.Node {
    constructor() {
        super("Set Energy");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("value", new ClassicPreset.Input(numSocket, "value"));
    }
}