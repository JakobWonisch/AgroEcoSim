import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class ApplyCrownPitchNode extends ClassicPreset.Node {
    constructor() {
        super("Apply Crown Pitch");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("crownPitch", new ClassicPreset.Input(numSocket, "crownPitch"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
