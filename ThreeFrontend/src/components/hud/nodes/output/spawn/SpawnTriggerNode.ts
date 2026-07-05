import { ClassicPreset } from "rete";
import { boolSocket } from "../../Sockets";

/** Base for spawn-* nodes (label set by subclass). */
export class SpawnTriggerNode extends ClassicPreset.Node {
    constructor(label: string) {
        super(label);
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
    }
}