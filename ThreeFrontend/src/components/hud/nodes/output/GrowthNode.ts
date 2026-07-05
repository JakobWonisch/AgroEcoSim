import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

/** Label "Growth"; input socket keys Length / Radius match server compiler. */
export class GrowthNode extends ClassicPreset.Node {
    constructor() {
        super("Growth");
        this.addInput("Length", new ClassicPreset.Input(numSocket, "Length"));
        this.addInput("Radius", new ClassicPreset.Input(numSocket, "Radius"));
    }
}