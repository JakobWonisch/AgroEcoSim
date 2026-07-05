import { SpawnTriggerNode } from "./SpawnTriggerNode";
import { boolSocket, numSocket } from "../../Sockets";
import { ClassicPreset } from "rete";

export class SpawnMeristemNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Meristem");
        this.addOutput("childId", new ClassicPreset.Output(numSocket, "child id"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}

export class SpawnDichotomousMeristemsNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Dichotomous Meristems");
        this.addOutput("childId1", new ClassicPreset.Output(numSocket, "child id 1"));
        this.addOutput("childId2", new ClassicPreset.Output(numSocket, "child id 2"));
        this.addOutput("lateralPitch", new ClassicPreset.Output(numSocket, "lateral pitch"));
        this.addOutput("seq", new ClassicPreset.Output(boolSocket, "seq"));
    }
}

export class SpawnBudNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Bud");
    }
}

export class SpawnStemNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Stem");
    }
}

export class SpawnFlowerStemNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Flower Stem");
    }
}

export class SpawnFlowerMeristemNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Flower Meristem");
    }
}

export class SpawnFlowerBudNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Flower Bud");
    }
}

export class SpawnFlowerPadelNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Flower Padel");
    }
}

export class SpawnRhizomeNode extends SpawnTriggerNode {
    constructor() {
        super("Spawn Rhizome");
    }
}
