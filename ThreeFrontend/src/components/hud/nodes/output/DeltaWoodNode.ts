import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class DeltaWoodNode extends ClassicPreset.Node {
    constructor() {
        super("Delta Wood");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }
}