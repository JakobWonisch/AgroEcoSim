import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class BecomeFlowerMeristemNode extends ClassicPreset.Node {
    constructor() {
        super("Become Flower Meristem");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}