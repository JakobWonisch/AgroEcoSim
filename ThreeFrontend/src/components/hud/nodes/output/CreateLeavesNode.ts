import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class CreateLeavesNode extends ClassicPreset.Node {
    constructor() {
        super("Create Leaves");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("meristemId", new ClassicPreset.Input(numSocket, "meristem id"));
        this.addInput("lateralAngle", new ClassicPreset.Input(numSocket, "lateral angle"));
        this.addInput("lateralsPerNode", new ClassicPreset.Input(numSocket, "laterals per node"));
        this.addInput("lateralRollVar", new ClassicPreset.Input(numSocket, "lateral roll var"));
        this.addInput("lateralPitchVar", new ClassicPreset.Input(numSocket, "lateral pitch var"));
        this.addInput("lateralPitch", new ClassicPreset.Input(numSocket, "lateral pitch"));
        this.addInput("leafPitch", new ClassicPreset.Input(numSocket, "leaf pitch"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
