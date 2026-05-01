import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

/**
 * Deterministic organ flags for the current agent (server). Socket names match legacy Agent Type node.
 * type1=Leaf, type2=Stem, type3=Meristem, type4=Petiole, type5=Bud
 */
export class OrganSensorsNode extends ClassicPreset.Node {
    constructor() {
        super("Organ Sensors");
        this.addOutput("type1", new ClassicPreset.Output(boolSocket, "Leaf"));
        this.addOutput("type2", new ClassicPreset.Output(boolSocket, "Stem"));
        this.addOutput("type3", new ClassicPreset.Output(boolSocket, "Meristem"));
        this.addOutput("type4", new ClassicPreset.Output(boolSocket, "Petiole"));
        this.addOutput("type5", new ClassicPreset.Output(boolSocket, "Bud"));
    }

    data() {
        return {
            type1: false,
            type2: false,
            type3: false,
            type4: false,
            type5: false,
        };
    }
}
