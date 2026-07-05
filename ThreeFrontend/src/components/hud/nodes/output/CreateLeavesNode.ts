import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class CreateLeavesNode extends ClassicPreset.Node {
    constructor() {
        super("Create Leaves");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("meristemId", new ClassicPreset.Input(numSocket, "meristem id"));
        this.addInput("lateralAngle", new ClassicPreset.Input(numSocket, "lateral angle"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}