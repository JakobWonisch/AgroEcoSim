import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class DeltaEnergyNode extends ClassicPreset.Node {
    constructor() {
        super("Delta Energy");
        this.addInput("amount", new ClassicPreset.Input(numSocket, "amount"));
    }
}