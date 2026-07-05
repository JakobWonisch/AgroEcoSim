import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class BecomeFlowerStemNode extends ClassicPreset.Node {
    constructor() {
        super("Become Flower Stem");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}