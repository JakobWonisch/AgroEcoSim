import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

/** Bergania chaining: bends every petiole from the parent up to the rhizome (legacy bendPetiol). */
export class BendAncestorPetiolesNode extends ClassicPreset.Node {
    constructor() {
        super("Bend Ancestor Petioles");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("petiolMoveDown", new ClassicPreset.Input(numSocket, "move down (×π)"));
        this.addInput("petiolMoveDownMax", new ClassicPreset.Input(numSocket, "max angle (×π)"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
