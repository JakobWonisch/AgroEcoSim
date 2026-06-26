import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class ParentInputNode extends ClassicPreset.Node {
    constructor() {
        super("Parent Input");
        this.addOutput("parentIsRhizome", new ClassicPreset.Output(boolSocket, "Is parent rhizome"));
        this.addOutput("parentWood", new ClassicPreset.Output(numSocket, "Parent wood"));
    }
}