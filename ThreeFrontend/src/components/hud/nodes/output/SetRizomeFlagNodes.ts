import { ClassicPreset } from "rete";
import { boolSocket } from "../Sockets";

class SetRizomeFlagNode extends ClassicPreset.Node {
    constructor(label: string) {
        super(label);
        this.addInput("value", new ClassicPreset.Input(boolSocket, "value"));
    }
}

export class SetRizomeTestNode extends SetRizomeFlagNode {
    constructor() {
        super("Set Rizome Test");
    }
}

export class SetRizomeTest2Node extends SetRizomeFlagNode {
    constructor() {
        super("Set Rizome Test2");
    }
}

export class SetRizomeTest3Node extends SetRizomeFlagNode {
    constructor() {
        super("Set Rizome Test3");
    }
}

export class SetRizomeTest4Node extends SetRizomeFlagNode {
    constructor() {
        super("Set Rizome Test4");
    }
}
