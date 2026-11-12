import { ClassicPreset } from "rete";
import { boolSocket, numSocket } from "../../Sockets";

/** Base for spawn-* nodes (label set by subclass). */
export class SpawnTriggerNode extends ClassicPreset.Node {
    constructor(label: string) {
        super(label);
        this.addInput("trigger", new ClassicPreset.Input(boolSocket, "trigger"));
        this.addInput("lateralRoll", new ClassicPreset.Input(numSocket, "lateral roll"));
        this.addInput("twigsBending", new ClassicPreset.Input(numSocket, "twig bending"));
        this.addInput("twigsBendingLevel", new ClassicPreset.Input(numSocket, "twig bending level"));
        this.addInput("twigsBendingApical", new ClassicPreset.Input(numSocket, "twig bending apical"));
        this.addInput("shootsGravitaxis", new ClassicPreset.Input(numSocket, "shoot gravitaxis"));
    }
}
