import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class SetEnergyToCapacityNode extends ClassicPreset.Node {
    constructor() {
        super("Set Energy To Capacity");
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}
