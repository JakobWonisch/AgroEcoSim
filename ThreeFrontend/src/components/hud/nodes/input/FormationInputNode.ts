import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../Sockets";

export class FormationInputNode extends ClassicPreset.Node {
    constructor() {
        super("Formation Input");
        this.addOutput("parentIsRhizome", new ClassicPreset.Output(boolSocket, "Parent is rhizome"));
        this.addOutput("parentWood", new ClassicPreset.Output(numSocket, "Parent wood"));
        this.addOutput("parentLeaf", new ClassicPreset.Output(boolSocket, "Parent is leaf"));
        this.addOutput("parentStem", new ClassicPreset.Output(boolSocket, "Parent is stem"));
        this.addOutput("parentMeristem", new ClassicPreset.Output(boolSocket, "Parent is meristem"));
        this.addOutput("parentPetiole", new ClassicPreset.Output(boolSocket, "Parent is petiole"));
        this.addOutput("parentBud", new ClassicPreset.Output(boolSocket, "Parent is bud"));
        this.addOutput("parentAuxins", new ClassicPreset.Output(numSocket, "Parent auxins"));
        this.addOutput("grandparentAuxins", new ClassicPreset.Output(numSocket, "Grandparent auxins"));
        this.addOutput("parentDominance", new ClassicPreset.Output(numSocket, "Parent dominance"));
        this.addOutput("parentBaseRadius", new ClassicPreset.Output(numSocket, "Parent base radius"));
        this.addOutput("hasChildren", new ClassicPreset.Output(boolSocket, "Has children"));
        this.addOutput("childrenProductionSum", new ClassicPreset.Output(numSocket, "Children production sum"));
        this.addOutput("agentHeightRatio", new ClassicPreset.Output(numSocket, "Agent height ratio"));
        this.addOutput("dailyProductionMax", new ClassicPreset.Output(numSocket, "Daily production max"));
        this.addOutput("dailyResourceMax", new ClassicPreset.Output(numSocket, "Daily resource max"));
        this.addOutput("dailyEfficiencyMax", new ClassicPreset.Output(numSocket, "Daily efficiency max"));
        this.addOutput("waterBalance", new ClassicPreset.Output(numSocket, "Water balance"));
        this.addOutput("energyProductionMax", new ClassicPreset.Output(numSocket, "Energy production max"));
        this.addOutput("auxinLocalMinimum", new ClassicPreset.Output(boolSocket, "Auxin local minimum"));
    }
}
