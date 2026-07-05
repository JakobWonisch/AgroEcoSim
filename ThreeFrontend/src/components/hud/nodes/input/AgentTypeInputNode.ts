import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

export class AgentTypeInputNode extends ClassicPreset.Node {
    constructor() {
        super("Agent Type Input");
        this.addOutput("leaf", new ClassicPreset.Output(boolSocket, "Is Leaf"));
        this.addOutput("stem", new ClassicPreset.Output(boolSocket, "Is Stem"));
        this.addOutput("meristem", new ClassicPreset.Output(boolSocket, "Is Meristem"));
        this.addOutput("petiole", new ClassicPreset.Output(boolSocket, "Is Petiole"));
        this.addOutput("bud", new ClassicPreset.Output(boolSocket, "Is Bud"));
        this.addOutput("flowerStem", new ClassicPreset.Output(boolSocket, "Is Flower Stem"));
        this.addOutput("flowerMeristem", new ClassicPreset.Output(boolSocket, "Is Flower Meristem"));
        this.addOutput("flowerBud", new ClassicPreset.Output(boolSocket, "Is Flower Bud"));
        this.addOutput("flowerPadel", new ClassicPreset.Output(boolSocket, "Is Flower Padel"));
        this.addOutput("flowerPetiol", new ClassicPreset.Output(boolSocket, "Is Flower Petiol"));
    }
}