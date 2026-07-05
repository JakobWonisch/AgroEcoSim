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
