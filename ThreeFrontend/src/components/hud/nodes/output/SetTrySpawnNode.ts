import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class SetTrySpawnNode extends ClassicPreset.Node {
    constructor() {
        super("Set trySpawn");
        this.addInput("value", new ClassicPreset.Input(boolSocket, "value"));
    }
}